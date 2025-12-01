using AI_Server.DTOs.MoodModel;
using AI_Server.Infrastructure.Models.MoodModel;
using AI_Server.Mapping;
using AI_Server.Repositories.GenericClassificationModelRepo.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AI_Server.Infrastructure.Models.IntentModel;


namespace AI_Server.Controllers
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
        [HttpPost]
        public IActionResult PredictMood([FromBody] MoodModelInDTO inputDTO)
        {
            MoodInput input=new MoodInput();
            _Mapper.Map(input, inputDTO);
            
            _MoodRepo.Normalize(input.Text);
            MoodOutput Response=_MoodRepo.Predict(input);
            
            MoodModelOutDTO ResponseDTO= _Mapper.Map<MoodModelOutDTO>(Response);

            return Ok(ResponseDTO);
        }
        [HttpPost("/IntentModel")]
        public IActionResult PredictIntent([FromBody] IntentModelInDTO inputDTO)
        {
            IntentInput input= _Mapper.Map<IntentInput>(inputDTO);

            _IntentRepo.Normalize(input.Instruction);
            IntentOutput Response=_IntentRepo.Predict(input);

            IntentModelOutDTO ResponseDTO = _Mapper.Map<IntentModelOutDTO>(Response);

            return Ok(ResponseDTO);
        }
    }
}
