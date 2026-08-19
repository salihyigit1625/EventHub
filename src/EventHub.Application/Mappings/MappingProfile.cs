using AutoMapper;
using EventHub.Application.DTOs.Events;
using EventHub.Application.DTOs.Profiles;
using EventHub.Application.DTOs.Ticketing;
using EventHub.Domain.Entities.Events;
using EventHub.Domain.Entities.Profiles;
using EventHub.Domain.Entities.Ticketing;

namespace EventHub.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateEventDto, Event>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title.Trim()))
            .ForMember(dest => dest.Venue, opt => opt.MapFrom(src => src.Venue.Trim()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description != null ? src.Description.Trim() : null));

        CreateMap<UpdateEventDto, Event>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title.Trim()))
            .ForMember(dest => dest.Venue, opt => opt.MapFrom(src => src.Venue.Trim()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description != null ? src.Description.Trim() : null));

        CreateMap<Event, EventDto>();
        CreateMap<Event, EventListItemDto>();

        CreateMap<CreateTicketTypeDto, TicketType>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.RemainingQuantity, opt => opt.MapFrom(src => src.TotalQuantity));

        CreateMap<UpdateTicketTypeDto, TicketType>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()));

        CreateMap<TicketType, TicketTypeDto>();
        CreateMap<Ticket, TicketDto>();
        CreateMap<Payment, PaymentDto>();
        CreateMap<Waitlist, WaitlistDto>();
        CreateMap<WalletTransaction, WalletTransactionDto>();
        CreateMap<Document, DocumentDto>();

        CreateMap<UpdateOrganizerProfileDto, OrganizerProfile>()
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.CompanyName.Trim()))
            .ForMember(dest => dest.TaxNumber, opt => opt.MapFrom(src =>
                string.IsNullOrWhiteSpace(src.TaxNumber) ? null : src.TaxNumber.Trim()));
    }
}
