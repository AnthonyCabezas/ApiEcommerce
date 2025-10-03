namespace ApiEcommerce.Reposiroty.IRepository;

public interface ICategoryRepository
{
    ICollection<Category> GetAllCategories();
    Category GetCategoryById(int id);
    bool CategoryExists(int id);
    bool CategoryExists(string name);

    bool DeleteCategory(Category category);
    bool CreateCategory(Category category);
    bool UpdateCategory(Category category);

    bool Save();

}
