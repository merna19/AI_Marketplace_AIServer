using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Embeddings.Services
{
    public class ProductService(string connectionString)
    {
        private readonly string _connectionString = connectionString;

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(@"
                SELECT 
                    p.Id, 
                    p.Name, 
                    p.Description,
                    ISNULL(c.Name, 'Uncategorized') as Category
                FROM Products p
                LEFT JOIN Categories c ON p.CategoryId = c.Id",
                connection
            );

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(new Product
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Category = reader.GetString(3)
                });
            }

            return products;
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(@"
                SELECT 
                    p.Id, 
                    p.Name, 
                    p.Description,
                    ISNULL(c.Name, 'Uncategorized') as Category
                FROM Products p
                LEFT JOIN Categories c ON p.CategoryId = c.Id
                WHERE p.Id = @Id",
                connection
            );
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Product
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Category = reader.GetString(3)
                };
            }

            return null;
        }
    }
}