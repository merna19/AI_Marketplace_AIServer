using AI_Server.DTOs.MoodModel;

using AutoMapper;
namespace AI_Server.Mapping
{
    public class MappingProfile: Profile 
    {
        public MappingProfile()
        {
            CreateMap<MoodModel.ModelInput, MoodModelInDTO>().AfterMap(
                (src, des)=> { src.Label = des.Prompt; })
                .ReverseMap();


            CreateMap<MoodModel.ModelOutput, MoodModelOutDTO>()
                .ForMember(dest => dest.Confidence, opt => opt.MapFrom(src => src.Score.Max()))
                .ForMember(dest => dest.LabelScores, opt => opt.MapFrom(src => src.Score))
                .ReverseMap();



        }
    }
}
