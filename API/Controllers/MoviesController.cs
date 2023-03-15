using API.services;
using Microsoft.AspNetCore.Mvc;
using Nest;
using System;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {

        private readonly string movieIndex = "movies";
        private readonly IElasticClient _elasticClient;

        // dictionary for fields. key is class attribute (lowercase), value is elastic field name
        private Dictionary<string, string> MovieFields = new Dictionary<string, string>(){
            {"movieid", "movieID"},
            {"title", "title"},
            {"movieimdbrating", "movieIMDbRating" },
            {"totalratingcount", "totalRatingCount"},
            {"totaluserreviews", "totalUserReviews" },
            {"totalcriticreviews", "totalCriticReviews" },
            {"metascore", "metaScore" },
            {"moviegenres", "movieGenres" },
            {"directors", "directors" },
            {"datepublished", "datePublished" },
            {"timestamp", "@timestamp" },
            {"creators", "creators" },
            {"maindtars", "mainStars" },
            {"duration", "duration" },
            {"movietrailer", "movieTrailer" },
            {"moviesposter", "moviePoster" }
        };

        // create elasticClient field thru injection
        public MoviesController(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;

        }

        [HttpGet("")] //api/movies
        public async Task<ActionResult<List<Movie>>> GetMovies()
        {
            var response = await _elasticClient.SearchAsync<Movie>(s => s
                .Index(movieIndex)
                .Query(q => q.MatchAll()));
            // returns all movies (actually defaults to first 10)

            return response.Documents.ToList();
        }

        /// <summary>
        /// Can search for an exact match of field to search terms. Matches on by characters using regex. Monk matches monkey.
        /// </summary>
        /// <param name="field">The field to search movies by. Must match the capitalization and spelling of the elasticsearch field, not the model's attribute.</param>
        /// <param name="searchTerms">An array of all the terms you want to search for.</param>
        /// <returns></returns>
        [HttpGet("multiqueryByChar")]
        public async Task<ActionResult<List<Movie>>> GetMovieDataByChar([FromQuery] string field, [FromQuery] string[] searchTerms)
        {
            string eField = "title"; // default
            try
            {
                eField = MovieFields[field.ToLower().Trim()];
            }
            catch (Exception e) { }

            Movie movieOBJ = new Movie();
            var response = await _elasticClient.SearchAsync<Movie>(s => s.Index(movieIndex).Query(q => searchByCharRaw.RegexpRequest(eField, movieOBJ, searchTerms)));
            return response.Documents.ToList();
        }

        /// <summary>
        /// Can search for an exact match of field to search terms. Matches on a word-by-word basis. Monkey matches monkey, but Monk does not match monkey.
        /// </summary>
        /// <param name="field">The field to search movies by. Must match the capitalization and spelling of the elasticsearch field, not the model's attribute.</param>
        /// <param name="searchTerms">An array of all the terms you want to search for.</param>
        /// <returns></returns>
        [HttpGet("multiqueryByToken")]
        public async Task<ActionResult<List<Movie>>> GetMovieDataByToken([FromQuery] string field, [FromQuery] string[] searchTerms)
        {
            string eField = "title"; // default
            try
            {
                eField = MovieFields[field.ToLower().Trim()];
            }
            catch (Exception e) { }

            Movie movieOBJ = new Movie();
            var response = await _elasticClient.SearchAsync<Movie>(s => s.Index(movieIndex).Query(q => multiQueryMatch.MatchRequest(eField, movieOBJ, searchTerms)));
            return response.Documents.ToList();
        }

        /// <summary>
        ///  Can read any review field and find all reviews that match a specific number OR fit within a passed range on the chosen field.
        /// </summary>
        /// <param name="field">The field within a review to search on.</param>
        /// <param name="specificNum">The exact number to match on the field</param>
        /// <param name="minNum">The lower bound on the field (inclusive)</param>
        /// <param name="maxNum">The higher bound on the field (inclusive)</param>
        /// <returns></returns>
        [HttpGet("minmaxByField")] //api/reviews/minmaxByField
        public async Task<ActionResult<List<Movie>>> GetMinMax([FromQuery] string field, [FromQuery] string specificNum, [FromQuery] float minNum, [FromQuery] float maxNum)
        {
            string eField = "metaScore"; // default
            try
            {
                eField = MovieFields[field.ToLower().Trim()];
            }
            catch (Exception e) { }

            Movie movieOBJ = new Movie();
            if (!string.IsNullOrEmpty(specificNum))
            {

                var response = await _elasticClient.SearchAsync<Movie>(s => s.Index(movieIndex).Query(q => multiQueryMatch.MatchRequest(eField, movieOBJ, specificNum)));
                return response.Documents.ToList();
            }
            else
            {
                if (minNum > maxNum)
                {
                    return BadRequest("The 'minRating' parameter must be less than 'maxRating'");
                }

                var response = await _elasticClient.SearchAsync<Movie>(s => s.Index(movieIndex).Query(q => minMaxService.RangeRequest(eField, movieOBJ, minNum, maxNum)));
                return response.Documents.ToList();
            }
        }

        [HttpPost("")]
        public async Task<string> Post(Movie value)
        {
            // TODO: create an autoincrementing function for movieID and ensure no two movies have the same ID
            // Date automatically filled out
            var response = await _elasticClient.IndexAsync<Movie>(value, x => x.Index(movieIndex)); // pass incoming value and index
            return response.Id; // Id created when making a post call
        }

        [HttpPost("json-upload")]
        public async Task<string> Upload(List<IFormFile> files)
        {
            string returnString = "";
            // validate the json, parse it, store each movie
            foreach (IFormFile file in files)
            {
                try
                {
                    var serializerSettings = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    };
                    string json = JsonHandler.ReadAsList(file);
                    var movies = JsonSerializer.Deserialize<List<Movie>>(json, serializerSettings);
                    try
                    {
                        foreach (Movie movie in movies)
                        {
                            movie.Timestamp = movie.DatePublished;
                            string response = await Post(movie);
                            returnString += response + '\n';
                        }
                    }
                    catch (Exception e) { returnString += "Failed to post movie.\n" + e; }
                }
                catch (Exception e) { returnString += "Invalid File. Make sure the file is JSON and not NDJSON\n" + e; }

            }
            return returnString;
        }

        [HttpDelete("{elasticId}")]
        // delete based on id (http://localhost:9200/movies/_search) -> find id
        public async void Delete(string elasticId)
        {
            var response = await _elasticClient.DeleteAsync<Movie>(elasticId, d => d
              .Index(movieIndex));

        }

        [HttpPut("{elasticId}")]
        public async Task<string> Put(string elasticId, Movie value)
        {
            // TODO: create an autoincrementing function for movieID and ensure no two movies have the same ID
            // Date automatically filled out
            var response = await _elasticClient.UpdateAsync<Movie>(elasticId, u => u
                .Index(movieIndex)
                .Doc(new Movie
                {
                    MovieID = value.MovieID,
                    Title = value.Title,
                    MovieIMDbRating = value.MovieIMDbRating,
                    TotalRatingCount = value.TotalRatingCount,
                    TotalUserReviews = value.TotalUserReviews,
                    TotalCriticReviews = value.TotalCriticReviews,
                    MetaScore = value.MetaScore,
                    MovieGenres = value.MainStars,
                    Directors = value.Directors,
                    DatePublished = value.DatePublished,
                    Timestamp = value.Timestamp,
                    Creators = value.Creators,
                    MainStars = value.MainStars,
                    Description = value.Description,
                    Duration = value.Duration,
                    MovieTrailer = value.MovieTrailer,
                    MoviePoster = value.MoviePoster
                }));

            return response.Id;
        }

    }
}
