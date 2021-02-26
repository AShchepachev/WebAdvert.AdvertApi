using AutoMapper;
using WebAdvert.AdvertApi.Models;

namespace WebAdvert.AdvertApi.Services
{
    public class AdvertProfile : Profile
    {
        public AdvertProfile()
        {
            CreateMap<AdvertModel, AdvertDbModel>();
        }
    }
}
