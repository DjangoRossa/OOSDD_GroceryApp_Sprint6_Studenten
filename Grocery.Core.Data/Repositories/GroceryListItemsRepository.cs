using Grocery.Core.Data.Helpers;
using Grocery.Core.Interfaces.Repositories;
using Grocery.Core.Models;
using Microsoft.Data.Sqlite;

namespace Grocery.Core.Data.Repositories
{
    /// <summary>
    /// Repository for managing grocery list items in the database.
    /// </summary>
    public class GroceryListItemsRepository : DatabaseConnection, IGroceryListItemsRepository
    {
        /// <summary>
        /// In-memory list of grocery list items.
        /// </summary>
        private readonly List<GroceryListItem> groceryListItems = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="GroceryListItemsRepository"/> class.
        /// Creates the GroceryListItem table if it does not exist and inserts initial data.
        /// </summary>
        public GroceryListItemsRepository()
        {
            CreateTable(@"CREATE TABLE IF NOT EXISTS GroceryListItem (
                            [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                            [GroceryListId] INTEGER NOT NULL,
                            [ProductId] INTEGER NOT NULL,
                            [Amount] INTEGER NOT NULL);");
            var insertQueries = new List<string> {
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(1, 1, 3)",
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(1, 2, 1)",
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(1, 3, 4)",
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(2, 1, 2)",
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(2, 2, 5)"
            };
            InsertMultipleWithTransaction(insertQueries);
            GetAll();
        }

        /// <summary>
        /// Retrieves all grocery list items from the database.
        /// </summary>
        /// <returns>A list of all <see cref="GroceryListItem"/> objects.</returns>
        public List<GroceryListItem> GetAll()
        {
            groceryListItems.Clear();
            string selectQuery = "SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                SqliteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    int groceryListId = reader.GetInt32(1);
                    int productId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    groceryListItems.Add(new(id, groceryListId, productId, amount));
                }
            }
            CloseConnection();
            return groceryListItems;
        }

        /// <summary>
        /// Retrieves all grocery list items for a specific grocery list ID.
        /// </summary>
        /// <param name="id">The grocery list ID.</param>
        /// <returns>A list of <see cref="GroceryListItem"/> objects for the specified grocery list.</returns>
        public List<GroceryListItem> GetAllOnGroceryListId(int id)
        {
            var allOnGroceryListId = new List<GroceryListItem>();
            string selectQuery = $"SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem WHERE GroceryListId = {id}";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int itemId = reader.GetInt32(0);
                        int groceryListId = reader.GetInt32(1);
                        int productId = reader.GetInt32(2);
                        int amount = reader.GetInt32(3);
                        allOnGroceryListId.Add(new GroceryListItem(itemId, groceryListId, productId, amount));
                    }
                }
            }
            CloseConnection();
            return allOnGroceryListId;
        }

        /// <summary>
        /// Adds a new grocery list item to the database.
        /// </summary>
        /// <param name="item">The <see cref="GroceryListItem"/> to add.</param>
        /// <returns>The added <see cref="GroceryListItem"/> with its assigned ID.</returns>
        public GroceryListItem Add(GroceryListItem item)
        {
            OpenConnection();
            string insertQuery = "INSERT INTO GroceryListItem (GroceryListId, ProductId, Amount) VALUES (@GroceryListId, @ProductId, @Amount);";
            using (SqliteCommand command = new(insertQuery, Connection))
            {
                command.Parameters.AddWithValue("@GroceryListId", item.GroceryListId);
                command.Parameters.AddWithValue("@ProductId", item.ProductId);
                command.Parameters.AddWithValue("@Amount", item.Amount);
                command.ExecuteNonQuery();
            }
            // Retrieve the ID of the newly added item
            int productId = 0;
            using (SqliteCommand command = new("SELECT last_insert_rowid();", Connection))
            {
                productId = Convert.ToInt32(command.ExecuteScalar());
            }
            CloseConnection();
            // Assign the ID to the object
            item.Id = productId;
            return item;
        }

        /// <summary>
        /// Deletes a grocery list item from the database.
        /// </summary>
        /// <param name="item">The <see cref="GroceryListItem"/> to delete.</param>
        /// <returns>The deleted <see cref="GroceryListItem"/>.</returns>
        public GroceryListItem? Delete(GroceryListItem item)
        {
            string deleteQuery = $"DELETE FROM GroceryListItem WHERE Id = {item.Id};";
            OpenConnection();
            Connection.ExecuteNonQuery(deleteQuery);
            CloseConnection();
            return item;
        }

        /// <summary>
        /// Retrieves a grocery list item by its ID.
        /// </summary>
        /// <param name="id">The ID of the grocery list item.</param>
        /// <returns>The <see cref="GroceryListItem"/> if found; otherwise, null.</returns>
        public GroceryListItem? Get(int id)
        {
            string selectQuery = $"SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem WHERE Id = {id}";
            GroceryListItem? gl = null;
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                SqliteDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    int Id = reader.GetInt32(0);
                    int groceryListId = reader.GetInt32(1);
                    int productId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    gl = (new(Id, groceryListId, productId, amount));
                }
            }
            CloseConnection();
            return gl;
        }

        /// <summary>
        /// Updates an existing grocery list item in the database.
        /// </summary>
        /// <param name="item">The <see cref="GroceryListItem"/> to update.</param>
        /// <returns>The updated <see cref="GroceryListItem"/>.</returns>
        public GroceryListItem? Update(GroceryListItem item)
        {
            int recordsAffected;
            string updateQuery = $"UPDATE GroceryListItem SET GroceryListId = @GroceryListId, ProductId = @ProductId, Amount = @Amount WHERE Id = {item.Id};";
            OpenConnection();
            using (SqliteCommand command = new(updateQuery, Connection))
            {
                command.Parameters.AddWithValue("GroceryListId", item.GroceryListId);
                command.Parameters.AddWithValue("ProductId", item.ProductId);
                command.Parameters.AddWithValue("Amount", item.Amount);

                recordsAffected = command.ExecuteNonQuery();
            }
            CloseConnection();
            return item;
        }
    }
}