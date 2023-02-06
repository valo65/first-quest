using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProductFilterApi.Models;

namespace FirstQuest.ProductsController
{

        [Route("api/[controller]")]
        [ApiController]
        public class ProductsController : ControllerBase
        {
            private static readonly HttpClient Client = new HttpClient();
            private const string Url = "http://www.mocky.io/v2/5e307edf3200005d00858b49";

            [HttpGet]
            public async Task<ActionResult<FilterResult>> Filter([FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] string size, [FromQuery] string highlight)
            {
                var products = await GetProductsAsync();

          
                if (minPrice.HasValue)
                {
                    products = products.Where(p => p.Price >= minPrice).ToList();
                }

                if (maxPrice.HasValue)
                {
                    products = products.Where(p => p.Price <= maxPrice).ToList();
                }

                if (!string.IsNullOrEmpty(size))
                {
                    products = products.Where(p => p.Size.equals(size, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var highlightWords = !string.IsNullOrEmpty(highlight) ? highlight.Split(',') : new string[0];
                foreach (var product in products)
                {
                    foreach (var highlightWord in highlightWords)
                    {
                        product.Description = product.Description.Replace(highlightWord, $"<em>{highlightWord}</em>");
                    }
                }

                var minMaxPrices = products.Count > 0 ? (min: products.Min(p => p.Price), max: products.Max(p => p.Price)) : (min: 0m, max: 0m);

                var sizes = products.Select(p => p.Size).Distinct().ToArray();

                var wordCount = new Dictionary<string, int>();
                foreach (var product in products)
                {
                    var words = product.Description.Split(' ');
                    foreach (var word in words)
                    {
                        if (!wordCount.ContainsKey(word))
                        {
                            wordCount[word] = 0;
                        }

                        wordCount[word]++;
                    }
                }

                var commonWords = wordCount
                    .OrderByDescending(wc => wc.Value)
                    .Where(wc => !StopWords.Contains(wc.Key))
                    .Take(10)
                    .Select(wc => wc.Key)
                    .ToArray();

                var result = new FilterResult
                {
                    Products = products,
                    Filter = new Filter
                    {
                        MinPrice = minMaxPrices.min,
                        MaxPrice = minMaxPrices.max,
                        Sizes = sizes,
                        CommonWords = commonWords
                    }
                };

                return Ok(result);
            }

            private static async Task<List<Product>> GetProductsAsync()
            {
                var response = await Client.GetAsync(Url);
                response.EnsureSuccessStatusCode();

                var productsJson = await response.Content.ReadAsStringAsync();
                var products = JsonConvert.DeserializeObject<List<Product>>(productsJson);

                return products;
            }
        }

        public class FilterResult
        {
            public List<Product> Products { get; set; }
            public Filter Filter { get; set; }
        }

        public class Filter
        {
            public decimal MinPrice { get; set; }
            public decimal MaxPrice { get; set; }
            public string[] Sizes { get; set; }
            public string[] CommonWords { get; set; }
        }

        public static class StopWords
        {
            public static readonly string[] Words =
            {
            "a", "an", "and", "are", "as", "at", "be", "by", "for", "from",
            "has", "he", "in", "is", "it", "its","of", "on", "that", "the",
            "to", "was", "were", "with"
        };
        }
}
