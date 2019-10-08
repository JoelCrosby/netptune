using AutoMapper;
using Netptune.Models.ViewModels.Users;

namespace Netptune.Models.MappingProfiles
{
    public class UserMaps : Profile
    {
        public UserMaps()
        {
            CreateMap<AppUser, UserViewModel>();
        }
    }
}
