using API;
using API.Controllers;
using API.services;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Mvc;
using Nest;
using System.Net;

namespace NUnitTesting
{
    [TestFixture]
    public abstract class AbstractTestBase
    {
        private IElasticClient _elasticClient;
        protected MoviesController _movieController;

        [SetUp]
        public void Setup()
        {
            //var config = new ConnectionConfiguration(new Uri("http://localhost:9200"));
            _elasticClient = new ElasticClient();
            _movieController = new MoviesController(_elasticClient);
        }
    }
}