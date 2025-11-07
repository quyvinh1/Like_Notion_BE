using AutoMapper;
using TaskManager.DTOs;
using TaskManager.Models;

namespace TaskManager.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateTaskDto, TodoItem>();
            CreateMap<UpdateTaskDto, TodoItem>();
            CreateMap<Category, CategoryDto>();
            CreateMap<Attachment, AttachmentDto>();
            CreateMap<ContentBlock, BlockDto>();
            CreateMap<TodoItem, PageSummaryDto>()
                .ForMember(dest => dest.HasChildren, opt => opt.MapFrom(src => src.Children != null && src.Children.Any()));
            CreateMap<TodoItem, PageDetailDto>()
                .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children));
            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User.Email));
        }
    }
}
