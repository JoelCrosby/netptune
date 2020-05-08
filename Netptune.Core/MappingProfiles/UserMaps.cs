using AutoMapper;

using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.MappingProfiles
{
    public class UserMaps : Profile
    {
        public UserMaps()
        {
            CreateMap<AppUser, UserViewModel>();
        }
    }
}
