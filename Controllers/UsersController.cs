using ApiEcommerce.Models.Dtos;
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
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }


        /// <summary>
        /// Retrieves the list of all users.
        /// </summary>
        /// <remarks>
        /// Returns a collection of <see cref="UserDto"/> objects.  
        /// 
        /// <b>Notes:</b>
        /// - The response is cached for 30 seconds using <c>[ResponseCache]</c>.  
        /// - Use this endpoint to fetch all available users in the system.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X GET "https://localhost:5136/api/v1/users" \
        ///   -H "Accept: application/json"
        /// </code>
        /// </remarks>
        /// <returns>HTTP 200 with a list of users.</returns>
        /// <response code="200">A collection of users was successfully returned.</response>
        /// <response code="403">Forbidden. The client does not have access to this resource.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)] 
        [ResponseCache(Duration = 30)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] 
        public IActionResult GetUsers()
        {
            var users = _userRepository.GetUsers();
            var usersDto = users.Adapt<List<UserDto>>();
            return Ok(usersDto);
        }

        /// <summary>
        /// Retrieves a specific user by their unique identifier.
        /// </summary>
        /// <remarks>
        /// Returns a <see cref="UserDto"/> if the user exists.  
        /// 
        /// <b>Notes:</b>
        /// - A valid user ID must be provided in the route.  
        /// - If the user does not exist, a 404 response is returned.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X GET "https://localhost:5136/api/v1/users/12345" \
        ///   -H "Accept: application/json"
        /// </code>
        /// </remarks>
        /// <param name="id">The unique identifier of the user.</param>
        /// <returns>HTTP 200 with the user details if found.</returns>
        /// <response code="200">The user was found and returned successfully.</response>
        /// <response code="400">The provided ID is invalid.</response>
        /// <response code="403">Forbidden. The client does not have access rights.</response>
        /// <response code="404">No user with the specified ID was found.</response>
        [HttpGet("{id}", Name = "GetUser")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetUser(string id)
        {
            var user = _userRepository.GetUser(id);
            if (user == null)
            {
                return NotFound($"The user with ID {id} does not exist");
            }
            var userDto = user.Adapt<UserDto>();
            return Ok(userDto);
        }

        /// <summary>
        /// Authenticates a user and returns an access token.
        /// </summary>
        /// <remarks>
        /// Validates the provided credentials and, if successful, returns authentication data 
        /// (such as a JWT token or session information).  
        /// 
        /// <b>Notes:</b>
        /// - This endpoint is public (<c>[AllowAnonymous]</c>).  
        /// - Requires a valid <see cref="UserLoginDto"/> with username/email and password.  
        /// - If authentication fails, a 401 response is returned.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X POST "https://localhost:5136/api/v1/users/login" \
        ///   -H "Content-Type: application/json" \
        ///   -d "{ \"username\": \"john.doe\", \"password\": \"Secret123\" }"
        /// </code>
        /// </remarks>
        /// <param name="userLoginDto">The user login credentials (username/email and password).</param>
        /// <returns>HTTP 200 with the authentication response if credentials are valid.</returns>
        /// <response code="200">Login successful. Returns authentication details.</response>
        /// <response code="400">Invalid request. The payload is null or fails validation.</response>
        /// <response code="401">Authentication failed. Invalid username or password.</response>
        /// <response code="403">Forbidden. The user is not allowed to access this resource.</response>
        /// <response code="500">An unexpected server error occurred during login.</response>
        [AllowAnonymous]
        [HttpPost("login", Name = "Login")]
        [ProducesResponseType(StatusCodes.Status200OK)] 
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        [ProducesResponseType(StatusCodes.Status403Forbidden)] 
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] 
        public async Task<IActionResult> Login([FromBody] UserLoginDto userLoginDto)
        {
            if (userLoginDto == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var response = await _userRepository.Login(userLoginDto);
            if (response == null)
            {
                return Unauthorized();
            }
            return Ok(response);
        }

        /// <summary>
        /// Checks whether a given username is unique.
        /// </summary>
        /// <remarks>
        /// Returns a boolean value indicating if the provided username is available for registration.  
        /// 
        /// <b>Notes:</b>
        /// - This endpoint is typically used during user registration to validate uniqueness.  
        /// - Returns <c>true</c> if the username does not exist, otherwise <c>false</c>.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X GET "https://localhost:5136/api/v1/users/IsUniqueUser/john doe" \
        ///   -H "Accept: application/json"
        /// </code>
        /// </remarks>
        /// <param name="username">The username to validate.</param>
        /// <returns>HTTP 200 with a boolean result (<c>true</c> if unique, <c>false</c> if already taken).</returns>
        /// <response code="200">Successfully checked. Returns a boolean result.</response>
        /// <response code="403">Forbidden. The client does not have access rights.</response>
        [HttpGet("IsUniqueUser/{username}")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult IsUniqueUser(string username)
        {
            var isUnique = _userRepository.IsUniqueUser(username);
            return Ok(isUnique);
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <remarks>
        /// Accepts a <see cref="CreateUserDto"/> payload and creates a new user if the username is unique.  
        /// 
        /// <b>Notes:</b>
        /// - This endpoint is public (<c>[AllowAnonymous]</c>).  
        /// - The username must be unique; otherwise, a 400 response is returned.  
        /// - On success, the new user is created and returned with a <c>Location</c> header pointing to <c>GetUser</c>.  
        /// 
        /// <b>Example (curl):</b>
        /// <code>
        /// curl -X POST "https://localhost:5136/api/v1/users/register" \
        ///   -H "Content-Type: application/json" \
        ///   -d "{ \"username\": \"john.doe\", \"password\": \"Secret123\", \"email\": \"john@example.com\" }"
        /// </code>
        /// </remarks>
        /// <param name="createUserDto">The user registration details.</param>
        /// <returns>HTTP 201 with the created user and location header.</returns>
        /// <response code="201">User registered successfully. Returns the new user resource.</response>
        /// <response code="400">Invalid payload or the username already exists.</response>
        /// <response code="403">Forbidden. The client does not have access rights.</response>
        /// <response code="500">Unexpected server error during registration.</response>
        [AllowAnonymous]
        [HttpPost("register",Name = "RegisterUser")]
        [ProducesResponseType(StatusCodes.Status201Created)] 
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterUser([FromBody] CreateUserDto createUserDto)
        {
            if (createUserDto == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!_userRepository.IsUniqueUser(createUserDto.Username))
            {
                return BadRequest($"The user {createUserDto.Username} already exists");
            }
            var result = await _userRepository.Register(createUserDto);
            if (result == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al registrar el usuario");
            }
            return CreatedAtRoute("GetUser", new { id = result.Id }, result);
        }
    }
}
