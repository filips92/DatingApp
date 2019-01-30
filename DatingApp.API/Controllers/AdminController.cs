using System.Threading.Tasks;
using DatingApp.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using DatingApp.API.Dtos;
using Microsoft.AspNetCore.Identity;
using DatingApp.API.Models;
using Microsoft.Extensions.Options;
using DatingApp.API.Helpers;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using AutoMapper;
using System.Collections.Generic;

namespace DatingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private DataContext _context;
        private UserManager<User> _userManager;
        private IDatingRepository _repository;
        private IMapper _mapper;
        private IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;

        public AdminController(DataContext context,
            UserManager<User> userManager,
            IDatingRepository repository,
            IOptions<CloudinarySettings> cloudinaryConfig,
            IMapper mapper)
        {
            _context = context;
            _userManager = userManager;
            _repository = repository;
            _mapper = mapper;
            _cloudinaryConfig = cloudinaryConfig;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("usersWithRoles")]
        public async Task<IActionResult> GetUsersWithRoles()
        {
            var userList = await (
                from user in _context.Users orderby user.UserName
                select new
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Roles = (from userRole in user.UserRoles
                                join role in _context.Roles
                                on userRole.RoleId
                                equals role.Id
                                select role.Name).ToList()
                }
            ).ToListAsync();
            return Ok(userList);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("editRoles/{userName}")]
        public async Task<IActionResult> EditRoles(string userName, RoleEditDto roleEditDto)
        {
            var user = await _userManager.FindByNameAsync(userName);

            var userRoles = await _userManager.GetRolesAsync(user);

            var selectedRoles = roleEditDto.RoleNames;

            selectedRoles = selectedRoles ?? new string[] {};
            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if (!result.Succeeded)
            {
                return BadRequest("Failed to add to roles");
            }

            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if (!result.Succeeded)
            {
                return BadRequest("Failed to remove the roles");
            }

            return Ok(await _userManager.GetRolesAsync(user));
        }

        //TODO: Get photos for moderation
        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photosForModeration")]
        public async Task<IActionResult> GetPhotosForModeration()
        {
            var photos = await _context.Photos.Include(p => p.User).IgnoreQueryFilters().Where(p => p.IsApproved == false).ToListAsync();

            var photosToReturn = _mapper.Map<IEnumerable<PhotoForReturnDto>>(photos);
            return Ok(photosToReturn.OrderBy(p => p.UserName).ThenByDescending(p => p.DateAdded));
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("photos/{photoId}/accept")]
        public async Task<IActionResult> AcceptPhoto(int photoId)
        {
            var photo = await _context.Photos.IgnoreQueryFilters().SingleOrDefaultAsync(p => p.Id == photoId);

            if (photo == null)
            {
                return NotFound();
            }

            photo.IsApproved = true;

            if (await _repository.SaveAll())
            {
                return Ok();
            }

            return BadRequest("Failed to accept the photo");
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("photos/{photoId}/reject")]
        public async Task<IActionResult> RejectPhoto(int photoId)
        {
            var photo = await _context.Photos.IgnoreQueryFilters().SingleOrDefaultAsync(p => p.Id == photoId);

            if (photo.PublicId != null)
            {
                var deleteParams = new DeletionParams(photo.PublicId);

                var result = _cloudinary.Destroy(deleteParams);

                if (result.Result == "ok") {
                    _repository.Delete(photo);
                }
            }

            if (photo.PublicId == null)
            {
                _repository.Delete(photo);
            }

            if (await _repository.SaveAll())
            {
                return Ok();
            }

            return BadRequest("Failed to reject the photo");
        }
    }
}