using Webapplication.Models;
using Webapplication.ViewModel;
using AutoMapper;

namespace Webapplication.Mapping
{
    public class MapProfile : Profile
    {
        public MapProfile()
        {
          
            CreateMap<AppUser,LoginViewModel>().ReverseMap();
            CreateMap<AppUser,RegisterViewModel>().ReverseMap();
            CreateMap<Category,CategoryViewModel>().ReverseMap();

          
        }
    }
}
