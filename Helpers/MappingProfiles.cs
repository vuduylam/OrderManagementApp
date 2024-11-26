using AutoMapper;
using OrderManagementApp.DTOs;
using OrderManagementApp.Models;

namespace OrderManagementApp.Helpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles() 
        {
            CreateMap<Category, CategoryDTO>().ForMember(des => des.CategoryId,o=>o.MapFrom(src=>src.CategoryId));

        }
    }
}
