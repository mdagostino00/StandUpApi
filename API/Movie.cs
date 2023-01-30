using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API
{
    // movie model class
    public class Movie
    {
        // data type reflects json in ES
        // public int Id { get; set; } // movieID
        public string Title { get; set; }
        public float movieIMDbRating { get; set; } // 0 - 10. UserRating
        public double TotalRatingCount { get; set; }
        public string TotalUserReviews { get; set; } // i.e., "9.5k". stored as string in dataset
        public int TotalCriticReviews { get; set; } // i.e., "593". stored as int in dataset
        public int MetaScore { get; set; } // 0 - 100. CriticRating.
        public string[] MovieGenres { get; set; }
        public string[] Directors { get; set; }
        public string DatePublished { get; set; } // i.e., "2019-04-26". stored as string in dataset
        public string[] Creators { get; set; }
        public string[] MainStars { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; } // in minutes
        public string MovieTrailer { get; set; }  // youtube link to be embedded
        public string MoviePoster { get; set; }
        //public string @timestamp { get; set; } // one day after date published. automatically assigned by elastic
    }
}