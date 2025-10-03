using Microsoft.AspNetCore.Mvc;
using ApiEcommerce.Reposiroty.IRepository;
using Mapster;
using ApiEcommerce.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using ApiEcommerce.Constants;
using Asp.Versioning;

namespace ApiEcommerce.Controllers.V1
{

    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiController]
    [Authorize(Roles = "admin")]
    //[EnableCors(PoliceNames.AllowSpecificOrigin)]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        public CategoriesController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        /// <summary>
        /// Retrieves all categories.
        /// </summary>
        /// <remarks>
        /// Returns a collection of <see cref="CategoryDto"/> objects.
        /// 
        /// <b>Notes:</b>
        /// - This endpoint is public.
        /// - Response is cached for 30 seconds (Default30 cache profile).
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X GET "https://localhost:5136/api/v1/categories" -H "Accept: application/json"
        /// </code>
        /// </remarks>
        /// <returns>HTTP 200 with a list of categories.</returns>
        /// <response code="200">A collection of categories was successfully returned.</response>
        /// <response code="403">Forbidden if access is denied.</response>
        [AllowAnonymous]
        [HttpGet]
        //[ResponseCache(Duration = 30)]
        //[ResponseCache(CacheProfileName = "Default30")]
        [ResponseCache(CacheProfileName = CacheProfiles.Default30)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] 
        //[Obsolete("Use GetCategoriesOrderByID in V2 instead, this method will be removed in future versions.")]
        public IActionResult GetCategories()
        {
            var categories = _categoryRepository.GetAllCategories();
            var categoriesDto = categories.Adapt<ICollection<CategoryDto>>();
            return Ok(categoriesDto);
        }

        /// <summary>
        /// Retrieves a specific category by its unique identifier.
        /// </summary>
        /// <remarks>
        /// Returns a <see cref="CategoryDto"/> if the category exists.
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X GET "https://localhost:5136/api/v1/categories/5" -H "Accept: application/json"
        /// </code>
        /// </remarks>
        /// <param name="id">The unique identifier of the category (must be greater than 0).</param>
        /// <returns>HTTP 200 with the category details if found.</returns>
        /// <response code="200">Category found and returned successfully.</response>
        /// <response code="400">Invalid request. The supplied ID is not valid.</response>
        /// <response code="403">Forbidden. The client does not have access rights.</response>
        /// <response code="404">Category with the specified ID does not exist.</response>
        [AllowAnonymous]
        [HttpGet("{id:int}", Name = "GetCategory")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] 
        [ProducesResponseType(StatusCodes.Status404NotFound)] 
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        public IActionResult GetCategory(int id)
        {
            var category = _categoryRepository.GetCategoryById(id);
            if (category == null)
            {
                return NotFound($"The category with ID {id} does not exist");
            }
            var categoryDto = category.Adapt<CategoryDto>();
            return Ok(categoryDto);
        }

        /// <summary>
        /// Creates a new category.
        /// </summary>
        /// <remarks>
        /// Accepts a <see cref="CreateCategoryDto"/> and creates a new category if it does not already exist.
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X POST "https://localhost:5136/api/v1/categories" \
        ///   -H "Content-Type: application/json" \
        ///   -d "{ \"name\": \"Accessories\" }"
        /// </code>
        /// </remarks>
        /// <param name="createCategoryDto">Payload containing the category data.</param>
        /// <returns>HTTP 201 with the created category and a Location header.</returns>
        /// <response code="201">Category created successfully. Returns the resource in the response body and the Location header.</response>
        /// <response code="400">Invalid payload. Returns validation details.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Forbidden. The client is authenticated but not authorized.</response>
        /// <response code="409">A category with the same name already exists.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)] 
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] 
        [ProducesResponseType(StatusCodes.Status403Forbidden)] 
        [ProducesResponseType(StatusCodes.Status404NotFound)] 
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] 
        public IActionResult CreateCategory([FromBody] CreateCategoryDto createCategoryDto)
        {
            if (createCategoryDto == null)
            {
                return BadRequest(ModelState);
            }

            if (_categoryRepository.CategoryExists(createCategoryDto.Name))
            {
                ModelState.AddModelError("CustomError", "The category already exists");
                return BadRequest(ModelState);
            }

            var category = createCategoryDto.Adapt<Category>();

            if (!_categoryRepository.CreateCategory(category))
            {
                ModelState.AddModelError("CustomError", $"Something went wrong while saving the category {category.Name}");
                return StatusCode(500, ModelState);
            }

            return CreatedAtRoute("GetCategory", new { id = category.Id }, category);
        }

        /// <summary>
        /// Partially updates an existing category by its ID.
        /// </summary>
        /// <remarks>
        /// Sends only the fields you want to modify. Fields omitted will remain unchanged.
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X PATCH "https://localhost:5136/api/v1/categories/7" \
        ///   -H "Content-Type: application/json" \
        ///   -d "{ \"name\": \"Peripherals\" }"
        /// </code>
        /// </remarks>
        /// <param name="id">The category ID (must be greater than 0).</param>
        /// <param name="updateCategoryDto">Partial payload with fields to update.</param>
        /// <returns>HTTP 204 on success.</returns>
        /// <response code="204">The category was updated successfully (no content returned).</response>
        /// <response code="400">Invalid request (e.g., null body or invalid fields).</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Forbidden. The client lacks permissions.</response>
        /// <response code="404">The category with the given ID does not exist.</response>
        /// <response code="409">A category with the provided name already exists.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpPatch("{id:int}", Name = "UpdateCategory")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] 
        [ProducesResponseType(StatusCodes.Status404NotFound)] 
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] 
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] 
        public IActionResult UpdateCategory(int id, [FromBody] CreateCategoryDto updateCategoryDto)
        {
            if (!_categoryRepository.CategoryExists(id))
            {
                return NotFound($"The category with ID {id} does not exist");
            }
            if (updateCategoryDto == null)
            {
                return BadRequest(ModelState);
            }

            if (_categoryRepository.CategoryExists(updateCategoryDto.Name))
            {
                ModelState.AddModelError("CustomError", "The category already exists");
                return BadRequest(ModelState);
            }

            var category = updateCategoryDto.Adapt<Category>();
            category.Id = id;
            if (!_categoryRepository.UpdateCategory(category))
            {
                ModelState.AddModelError("CustomError", $"Something went wrong while updating the category {category.Name}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }

        /// <summary>
        /// Deletes an existing category by its ID.
        /// </summary>
        /// <remarks>
        /// Permanently removes the category resource identified by the provided ID.  
        /// 
        /// <b>Notes:</b>
        /// - Requires a valid category ID.  
        /// - If the category does not exist, a 404 response is returned.  
        /// - If the deletion fails unexpectedly, a 500 response is returned.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X DELETE "https://localhost:5136/api/v1/categories/3"
        /// </code>
        /// </remarks>
        /// <param name="id">The unique identifier of the category to delete.</param>
        /// <returns>No content if the deletion succeeds.</returns>
        /// <response code="204">The category was deleted successfully (no content returned).</response>
        /// <response code="400">Invalid request (e.g., negative or zero ID).</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Forbidden. The client is authenticated but not authorized.</response>
        /// <response code="404">The category with the given ID was not found.</response>
        /// <response code="500">An unexpected server error occurred while deleting the category.</response>
        [HttpDelete("{id:int}", Name = "DeleteCategory")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] 
        [ProducesResponseType(StatusCodes.Status404NotFound)] 
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] 
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] 
        public IActionResult DeleteCategory(int id)
        {
            if (!_categoryRepository.CategoryExists(id))
            {
                return NotFound($"The category with ID {id} does not exist");
            }
            var category = _categoryRepository.GetCategoryById(id);
            if (category == null)
            {
                return NotFound($"The category with ID {id} does not exist");
            }

            if (!_categoryRepository.DeleteCategory(category))
            {
                ModelState.AddModelError("CustomError", $"Something went wrong while deleting the category {category.Name}");
                return StatusCode(500, ModelState);
            }

            return NoContent();
        }

    }
}
