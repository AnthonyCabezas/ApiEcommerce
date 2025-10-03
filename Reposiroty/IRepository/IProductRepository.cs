using System;
using ApiEcommerce.Models;

namespace ApiEcommerce.Reposiroty.IRepository;

public interface IProductRepository
{
    ICollection<Product> GetAllProducts();
    ICollection<Product> GetProductsInPages(int pageNumber, int pageSize);
    int GetTotalProducts();
    Product? GetProductById(int id);
    ICollection<Product> GetProductsForCategory(int categoryId);
    ICollection<Product> SearchProducts(string searchTerm);

    bool BuyProduct(string name, int quantity);
    bool ProductExists(int id);
    bool ProductExists(string name);
    bool DeleteProduct(Product product);
    bool CreateProduct(Product product);
    bool UpdateProduct(Product product);

    bool Save();

}
