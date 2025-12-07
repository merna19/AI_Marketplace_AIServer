using AI_Server.Infrastructure.Models.MoodModel;
using AutoMapper;
using AI_Server.Infrastructure.Models.IntentModel;
using AI_Server.Application.DTOs.IntentModel;
using AI_Server.Application.DTOs.MoodModel;
namespace AI_Server.Application.Mapping
{
    public class MappingProfile: Profile 
    {
        public MappingProfile()
        {
            CreateMap<MoodInput, MoodModelInDTO>().AfterMap(
                (src, des)=> { src.Text = des.Prompt; })
                .ReverseMap();


            CreateMap<MoodOutput, MoodModelOutDTO>()
                .ForMember(dest => dest.Confidence, opt => opt.MapFrom(src => src.Scores.Max()))
                .ForMember(dest => dest.LabelScores, opt => opt.MapFrom(src => src.Scores))
                .ReverseMap();

            //CreateMap<IntentInput, IntentModelInDTO>().AfterMap(
            //    (src, des) => { src.Instruction = des.Prompt; })
            //    .ReverseMap();
            CreateMap<IntentInput, IntentModelInDTO>()
                .ForMember(dest => dest.Prompt, opt => opt.MapFrom(src=>src.Instruction))
                .ReverseMap();


            CreateMap<IntentOutput, IntentModelOutDTO>()
                .ForMember(dest => dest.Confidence, opt => opt.MapFrom(src => src.Scores.Max()))
                .ForMember(dest => dest.LabelScores, opt => opt.MapFrom(src => src.Scores))
                .ReverseMap();

        }
    }
}
