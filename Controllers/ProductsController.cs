using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Models.Dtos.Responses;
using ApiEcommerce.Reposiroty.IRepository;
using Asp.Versioning;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiEcommerce.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        public ProductsController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        /// <summary>
        /// Retrieves the list of all products.
        /// </summary>
        /// <remarks>
        /// Returns a collection of <see cref="ProductDto"/> objects.  
        /// 
        /// <b>Notes:</b>
        /// - This endpoint returns all products in the system.  
        /// - Use filters or pagination if available in future versions to optimize large result sets.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X GET "https://localhost:5136/api/v1/products" \
        ///   -H "Accept: application/json"
        /// </code>
        /// </remarks>
        /// <returns>HTTP 200 with the list of products.</returns>
        /// <response code="200">A collection of products was successfully returned.</response>
        /// <response code="403">Forbidden. The client does not have access rights.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult GetProducts()
        {
            var products = _productRepository.GetAllProducts();
            var productsDto = products.Adapt<List<ProductDto>>();
            return Ok(productsDto);
        }

        /// <summary>
        /// Retrieves a specific product by its unique identifier.
        /// </summary>
        /// <remarks>
        /// Returns a <see cref="ProductDto"/> if the product exists.  
        /// 
        /// <b>Notes:</b>
        /// - A valid product ID must be provided in the route.  
        /// - If the product does not exist, a 404 response is returned.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X GET "https://localhost:5136/api/v1/products/42" \
        ///   -H "Accept: application/json"
        /// </code>
        /// </remarks>
        /// <param name="productId">The unique identifier of the product.</param>
        /// <returns>HTTP 200 with the product details if found.</returns>
        /// <response code="200">The product was found and returned successfully.</response>
        /// <response code="400">Invalid request. The provided product ID is not valid.</response>
        /// <response code="403">Forbidden. The client does not have access rights.</response>
        /// <response code="404">No product with the specified ID was found.</response>
        [HttpGet("{productId:int}", Name = "GetProduct")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetProduct(int productId)
        {
            var product = _productRepository.GetProductById(productId);
            if (product == null)
            {
                return NotFound($"The product with ID {productId} does not exist");
            }
            var productDto = product.Adapt<ProductDto>();
            return Ok(productDto);
        }

        /// <summary>
        /// Retrieves products in a paginated format.
        /// </summary>
        /// <remarks>
        /// Returns a paged list of <see cref="ProductDto"/> objects.  
        /// 
        /// <b>Notes:</b>
        /// - Pagination is controlled with <c>pageNumber</c> and <c>pageSize</c>.  
        /// - Both parameters must be greater than zero.  
        /// - If the requested page exceeds the total number of pages, a 400 response is returned.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X GET "https://localhost:5136/api/v1/products/paged?pageNumber=2&pageSize=10" \
        ///   -H "Accept: application/json"
        /// </code>
        /// 
        /// <b>Sample Response:</b>
        /// <code>
        /// {
        ///   "pageNumber": 2,
        ///   "pageSize": 10,
        ///   "totalPages": 5,
        ///   "items": [
        ///     { "id": 11, "name": "Product A", "price": 19.99 },
        ///     { "id": 12, "name": "Product B", "price": 29.99 }
        ///   ]
        /// }
        /// </code>
        /// </remarks>
        /// <param name="pageNumber">The current page number (must be greater than 0).</param>
        /// <param name="pageSize">The number of items per page (must be greater than 0).</param>
        /// <returns>HTTP 200 with a paginated list of products.</returns>
        /// <response code="200">A paginated list of products was successfully returned.</response>
        /// <response code="400">Invalid request. Page number or size is invalid, or exceeds available pages.</response>
        /// <response code="403">Forbidden. The client does not have access rights.</response>
        /// <response code="404">No products were found for the specified page.</response>
        [HttpGet("Paged", Name = "GetProductsInPages")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetProductInPages([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                return BadRequest("Page number and page size must be greater than 0");
            }
            var GetTotalProducts = _productRepository.GetTotalProducts();
            var totalPages = (int)Math.Ceiling((double)GetTotalProducts / pageSize);
            if(pageNumber > totalPages)
            {
                return BadRequest($"Page number {pageNumber} exceeds total pages {totalPages}");
            }
            var products = _productRepository.GetProductsInPages(pageNumber, pageSize);
            var productDto = products.Adapt<List<ProductDto>>();
            var paginationResponse = new PaginationResponse<ProductDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Items = productDto
            };
            return Ok(paginationResponse);
        }

        /// <summary>
        /// Creates a new product.
        /// </summary>
        /// <remarks>
        /// Accepts a <see cref="CreateProductDto"/> payload and creates a new product if it does not already exist.  
        /// 
        /// <b>Notes:</b>
        /// - The product name must be unique.  
        /// - The referenced category must exist; otherwise, a 400 response is returned.  
        /// - If no image is provided, a placeholder image is automatically assigned.  
        /// - On success, the new product is returned with a <c>Location</c> header pointing to <c>GetProduct</c>.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X POST "https://localhost:5136/api/v1/products" \
        ///   -H "Content-Type: application/json" \
        ///   -d "{
        ///         \"name\": \"Wireless Mouse\",
        ///         \"description\": \"Ergonomic wireless mouse\",
        ///         \"price\": 29.99,
        ///         \"categoryId\": 3,
        ///         \"image\": \"base64string\"
        ///       }"
        /// </code>
        /// </remarks>
        /// <param name="createProductDto">The product creation details.</param>
        /// <returns>HTTP 201 with the created product and a location header.</returns>
        /// <response code="201">Product created successfully. Returns the created product resource.</response>
        /// <response code="400">Invalid payload, duplicate product, or non-existent category.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Forbidden. The client does not have access rights.</response>
        /// <response code="404">The referenced category could not be found.</response>
        /// <response code="500">An unexpected server error occurred while saving the product.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult CreateProduct([FromBody] CreateProductDto createProductDto)
        {
            if (createProductDto == null)
            {
                return BadRequest(ModelState);
            }

            if (_productRepository.ProductExists(createProductDto.Name))
            {
                ModelState.AddModelError("CustomError", "The product already exists.");
                return BadRequest(ModelState);
            }
            if (!_categoryRepository.CategoryExists(createProductDto.CategoryId))
            {
                ModelState.AddModelError("CustomError", $"The category with ID {createProductDto.CategoryId} does not exist");
                return BadRequest(ModelState);
            }

            var product = createProductDto.Adapt<Product>();
            //add image upload
            if (createProductDto.Image != null)
            {
                UploadProductImage(createProductDto, product);
            }
            else
            {
                product.ImgUrl = "https://placehold.co/300x300";
            }
            if (!_productRepository.CreateProduct(product))
            {
                ModelState.AddModelError("CustomError", $"Something went wrong while saving the record {product.Name}");
                return StatusCode(500, ModelState);
            }
            var createdProduct = _productRepository.GetProductById(product.ProductId);
            var productDto = createdProduct.Adapt<ProductDto>();
            return CreatedAtRoute("GetProduct", new { productId = product.ProductId }, productDto);
        }

        /// <summary>
        /// Retrieves all products for a specific category.
        /// </summary>
        /// <remarks>
        /// Returns a collection of <see cref="ProductDto"/> objects belonging to the specified category.  
        /// 
        /// <b>Notes:</b>
        /// - A valid category ID must be provided.  
        /// - If no products exist for the category, a 404 response is returned.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X GET "https://localhost:5136/api/v1/products/GetProductsForCategory/3" \
        ///   -H "Accept: application/json"
        /// </code>
        /// </remarks>
        /// <param name="categoryId">The unique identifier of the category.</param>
        /// <returns>HTTP 200 with the list of products for the specified category.</returns>
        /// <response code="200">Products were found and returned successfully.</response>
        /// <response code="400">Invalid request. The provided category ID is not valid.</response>
        /// <response code="403">Forbidden. The client does not have access rights.</response>
        /// <response code="404">No products were found for the specified category.</response>
        [HttpGet("GetProductsForCategory/{categoryId:int}", Name = "GetProductsForCategory")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetProductsForCategory(int categoryId)
        {
            var products = _productRepository.GetProductsForCategory(categoryId);
            if (products == null || products.Count == 0)
            {
                return NotFound($"There are no products in the category with ID {categoryId}.");
            }
            var productsDto = products.Adapt<List<ProductDto>>();
            return Ok(productsDto);
        }

        /// <summary>
        /// Searches products by name or description.
        /// </summary>
        /// <remarks>
        /// Returns a collection of <see cref="ProductDto"/> objects that match the provided search term.  
        /// 
        /// <b>Notes:</b>
        /// - The search term is required and is case-insensitive.  
        /// - If no products match, a 404 response is returned.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X GET "https://localhost:5136/api/v1/products/SearchProduct/mouse" \
        ///   -H "Accept: application/json"
        /// </code>
        /// </remarks>
        /// <param name="searchTerm">The term to search for in product names and descriptions.</param>
        /// <returns>HTTP 200 with a list of matching products.</returns>
        /// <response code="200">Products matching the search term were returned successfully.</response>
        /// <response code="400">Invalid request. The search term is missing or invalid.</response>
        /// <response code="403">Forbidden. The client does not have access rights.</response>
        /// <response code="404">No products matched the given search term.</response>
        [HttpGet("SearchProduct/{searchTerm}", Name = "SearchProducts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult SearchProducts(string searchTerm)
        {
            var products = _productRepository.SearchProducts(searchTerm);
            if (products == null || products.Count == 0)
            {
                return NotFound($"There are no products containing the name or description {searchTerm}");
            }
            var productsDto = products.Adapt<List<ProductDto>>();
            return Ok(productsDto);
        }

        /// <summary>
        /// Purchases a specified quantity of a product by name.
        /// </summary>
        /// <remarks>
        /// Attempts to decrease the stock of the specified product by the given quantity.  
        /// 
        /// <b>Notes:</b>
        /// - The product name must exist in the system.  
        /// - The quantity must be greater than zero.  
        /// - If there is insufficient stock or the product cannot be purchased, a 400 response is returned.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X PATCH "https://localhost:5136/api/v1/products/BuyProduct/Keyboard/2" \
        ///   -H "Accept: application/json"
        /// </code>
        /// 
        /// <b>Sample Success Response:</b>
        /// <code>
        /// "The purchase of 2 units of product Keyboard was successful"
        /// </code>
        /// </remarks>
        /// <param name="name">The name of the product to purchase.</param>
        /// <param name="quantity">The number of units to buy (must be greater than 0).</param>
        /// <returns>HTTP 200 with a confirmation message if the purchase succeeds.</returns>
        /// <response code="200">The product purchase was successful.</response>
        /// <response code="400">Invalid request (null/empty product name, quantity ≤ 0, or insufficient stock).</response>
        /// <response code="403">Forbidden. The client does not have access rights.</response>
        /// <response code="404">The product with the specified name was not found.</response>
        [HttpPatch("BuyProduct/{name}/{quantity:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult BuyProduct(string name, int quantity)
        {
            if (string.IsNullOrWhiteSpace(name) || quantity <= 0)
            {
                return BadRequest("The product name or quantity provided is invalid");
            }
            if (!_productRepository.ProductExists(name))
            {
                return NotFound($"The product with name '{name}' does not exist");
            }
            if (quantity <= 0)
            {
                return BadRequest($"The quantity must be greater than 0");
            }
            var success = _productRepository.BuyProduct(name, quantity);
            if (!success)
            {
                ModelState.AddModelError("CustomError", $"The product {name} could not be purchased or there is not enough stock to complete the purchase");
                return BadRequest(ModelState);
            }
            var units = quantity == 1 ? "unit" : "units";
            return Ok($"The purchase of {quantity} {units} of product {name} was successful");
        }

        /// <summary>
        /// Updates an existing product by its ID.
        /// </summary>
        /// <remarks>
        /// Accepts an <see cref="UpdateProductDto"/> payload and updates the specified product if it exists.  
        /// 
        /// <b>Notes:</b>
        /// - The product must already exist; otherwise, a 400 response is returned.  
        /// - The provided category ID must exist.  
        /// - If no image is provided, a default placeholder image will be assigned.  
        /// - On success, no content is returned (204).  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X PUT "https://localhost:5136/api/v1/products/42" \
        ///   -H "Content-Type: application/json" \
        ///   -d "{
        ///         \"name\": \"Gaming Keyboard\",
        ///         \"description\": \"Mechanical RGB gaming keyboard\",
        ///         \"price\": 99.99,
        ///         \"categoryId\": 3,
        ///         \"image\": \"base64string\"
        ///       }"
        /// </code>
        /// </remarks>
        /// <param name="productId">The ID of the product to update.</param>
        /// <param name="updateProductDto">The updated product details.</param>
        /// <returns>No content if the update succeeds.</returns>
        /// <response code="204">The product was updated successfully (no content returned).</response>
        /// <response code="400">Invalid payload, product does not exist, or category not found.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Forbidden. The client does not have access rights.</response>
        /// <response code="500">An unexpected server error occurred while updating the product.</response>
        [HttpPut("{productId:int}", Name = "UpdateProduct")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult UpdateProduct(int productId, [FromBody] UpdateProductDto updateProductDto)
        {
            if (updateProductDto == null)
            {
                return BadRequest(ModelState);
            }

            if (!_productRepository.ProductExists(productId))
            {
                ModelState.AddModelError("CustomError", "The product does not exist");
                return BadRequest(ModelState);
            }
            if (!_categoryRepository.CategoryExists(updateProductDto.CategoryId))
            {
                ModelState.AddModelError("CustomError", $"The category with ID {updateProductDto.CategoryId} does not exist");
                return BadRequest(ModelState);
            }

            var product = updateProductDto.Adapt<Product>();
            product.ProductId = productId;
            //add image upload
            if (updateProductDto.Image != null)
            {
                UploadProductImage(updateProductDto, product);
            }
            else
            {
                product.ImgUrl = "https://placehold.co/300x300";
            }
            if (!_productRepository.UpdateProduct(product))
            {
                ModelState.AddModelError("CustomError", $"Something went wrong while updating the record {product.Name}");
                return StatusCode(500, ModelState);
            }
            return NoContent();
        }

        /// <summary>
        /// Handles the upload of a product image and updates the product entity with image paths.
        /// </summary>
        /// <remarks>
        /// Saves the uploaded image file into the <c>wwwroot/ProductsImages</c> folder.  
        /// 
        /// <b>Behavior:</b>
        /// - Generates a unique file name using the product ID and a GUID.  
        /// - Creates the <c>ProductsImages</c> folder if it does not exist.  
        /// - Deletes any file with the same name before saving the new one.  
        /// - Updates the product with both a public URL (<c>ImgUrl</c>) and a local file system path (<c>ImgUrlLocal</c>).  
        /// 
        /// <b>Example URL generated:</b>
        /// <code>
        /// https://localhost:5001/ProductsImages/42aabbccddeeff.jpg
        /// </code>
        /// </remarks>
        /// <param name="productDto">The DTO containing the uploaded image (expected to have <c>Image.FileName</c> and <c>Image.CopyTo()</c>).</param>
        /// <param name="product">The product entity to update with image paths.</param>
        private void UploadProductImage(dynamic productDto, Product product)
        {
            string fileName = product.ProductId + Guid.NewGuid().ToString() + Path.GetExtension(productDto.Image.FileName);
            var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ProductsImages");
            if (!Directory.Exists(imagesFolder))
            {
                Directory.CreateDirectory(imagesFolder);
            }
            var filePath = Path.Combine(imagesFolder, fileName);
            FileInfo file = new FileInfo(filePath);
            if (file.Exists)
            {
                file.Delete();
            }
            using var fileStream = new FileStream(filePath, FileMode.Create);
            productDto.Image.CopyTo(fileStream);
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
            product.ImgUrl = $"{baseUrl}/ProductsImages/{fileName}";
            product.ImgUrlLocal = filePath;
        }

        /// <summary>
        /// Deletes an existing product by its ID.
        /// </summary>
        /// <remarks>
        /// Permanently removes the product resource identified by the given ID.  
        /// 
        /// <b>Notes:</b>
        /// - A valid product ID must be provided.  
        /// - If the product does not exist, a 404 response is returned.  
        /// - If an error occurs during deletion, a 500 response is returned.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X DELETE "https://localhost:5136/api/v1/products/42"
        /// </code>
        /// </remarks>
        /// <param name="productId">The unique identifier of the product to delete.</param>
        /// <returns>No content if the deletion succeeds.</returns>
        /// <response code="204">The product was successfully deleted (no content returned).</response>
        /// <response code="400">Invalid request. The provided product ID is not valid.</response>
        /// <response code="403">Forbidden. The client does not have access rights.</response>
        /// <response code="404">The product with the given ID was not found.</response>
        /// <response code="500">An unexpected server error occurred while deleting the product.</response>
        [HttpDelete("{productId:int}", Name = "DeleteProduct")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DeleteProduct(int productId)
        {
            if (productId == 0)
            {
                return BadRequest(ModelState);
            }

            var product = _productRepository.GetProductById(productId);
            if (product == null)
            {
                return NotFound($"The product with ID {productId} does not exist");
            }
            if (!_productRepository.DeleteProduct(product))
            {
                ModelState.AddModelError("CustomError", $"Something went wrong while deleting the record {product.Name}");
                return StatusCode(500, ModelState);
            }
            return NoContent();
        }
    }
}
