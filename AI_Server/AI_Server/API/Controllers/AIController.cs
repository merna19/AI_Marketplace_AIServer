using AI_Server.Infrastructure.Interfaces.MoodModel;
using AI_Server.Application.Mapping;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AI_Server.Infrastructure.Interfaces.IntentModel;
using AI_Server.Application.DTOs.IntentModel;
using AI_Server.Application.DTOs.MoodModel;
using AI_Server.Infrastructure.Repositories.GenericClassificationModelRepo.Interfaces;


namespace AI_Server.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIController : ControllerBase
    {
        private IMapper _Mapper;
        private IGenericClassificationModel<MoodOutput, MoodInput> _MoodRepo;
        private IGenericClassificationModel<IntentOutput, IntentInput> _IntentRepo;
        public AIController(IGenericClassificationModel<MoodOutput, MoodInput> MoodRepo, IGenericClassificationModel<IntentOutput, IntentInput> IntentRepo, IMapper Mapper) 
        {
            this._MoodRepo = MoodRepo;
            this._Mapper = Mapper;
            this._IntentRepo = IntentRepo;
        }
        [HttpPost("/MoodModel")]
        public IActionResult PredictMood([FromBody] MoodModelInDTO inputDTO)
        {
            MoodInput input=_Mapper.Map<MoodInput>(inputDTO);
            
            _MoodRepo.Normalize(input.Prompt);
            MoodOutput Response=_MoodRepo.Predict(input);
            
            MoodModelOutDTO ResponseDTO= _Mapper.Map<MoodModelOutDTO>(Response);

            return Ok(ResponseDTO);
        }
        [HttpPost("/IntentModel")]
        public IActionResult PredictIntent([FromBody] IntentModelInDTO inputDTO)
        {
            IntentInput input= _Mapper.Map<IntentInput>(inputDTO);

            _IntentRepo.Normalize(input.Prompt);
            IntentOutput Response=_IntentRepo.Predict(input);

            IntentModelOutDTO ResponseDTO = _Mapper.Map<IntentModelOutDTO>(Response);

            return Ok(ResponseDTO);
        }
    }
}
