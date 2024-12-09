using AutoMapper;
using Game_API.Dtos.Notification;
using Game_API.Models.Auth;
using Game_API.Models.Notification;

namespace Game_API.Mappings
{
    public class NotificationMappingProfile : Profile
    {
        public NotificationMappingProfile()
        {
            CreateMap<Notification, NotificationDto>();
            CreateMap<User, UserNotificationDto>();
        }
    }
}
