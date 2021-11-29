using System;
using System.Collections.Generic;
using AutoMapper;
using Commander.Data;
using Commander.Dtos;
using Commander.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

// json object has camelCase but Asp net has PascalCase. this auto converted. explanation: http://www.binaryintellect.net/articles/a1e0e49e-d4d0-4b7c-b758-84234f14047b.aspx

// SQL PRIMARY KEY VALUE JUMP by 1000: It jumps because i restarted or forces shut down the server. explanation: https://social.msdn.microsoft.com/Forums/sqlserver/en-US/3d256650-0e94-4d0f-8b52-0ba6e1903215/primary-key-auto-incrementing-by-1000-instead-of-1?forum=transactsql

namespace Commander.Controllers
{
  [Route("api/commands")] // api/[controller] replaces with the class name of controller (here its CommandsController)
    [ApiController] //Controllers decorated with this attribute are configured with features and behavior targeted at improving the developer experience for building APIs.
  public class CommandsController : ControllerBase //base class for an MVC controller without view support.
    {
    private readonly ICommanderRepo _repository ;
    private readonly IMapper _mapper;

      public CommandsController(ICommanderRepo repository,IMapper mapper){ // respository is the inteerface of our dataacess class
          _repository = repository;
           _mapper = mapper;
      }
      //GET api/commands
      [HttpGet]
      public ActionResult <IEnumerable<Command>> GetAllCommands() {
        var commandItems = _repository.GetAllCommands();
        return Ok(_mapper.Map<IEnumerable<CommandReadDto>>(commandItems)); // changes a given type to a specified type in profile
      }        

      //GET api/commands/{id}
      [HttpGet("{id}",Name="GetCommandById")]
      public ActionResult <Command> Command(int id){
        var commandItem = _repository.GetCommandById(id);
        if(commandItem != null)
            {
                return Ok(_mapper.Map<CommandReadDto>(commandItem));
            }
        return NotFound();
      }        
      
      //POST api/commands
        [HttpPost]
        public ActionResult <CommandReadDto> CreateCommand(CommandCreateDto commandCreateDto)
        {
            var commandModel = _mapper.Map<Command>(commandCreateDto); // CommandCreateDto -> Command
            _repository.CreateCommand(commandModel);
            _repository.SaveChanges();
            var commandReadDto = _mapper.Map<CommandReadDto>(commandModel); // Command -> CommandReadDTo

            return CreatedAtRoute("GetCommandById", new {Id = commandReadDto.Id}, commandReadDto)
            ;// Route values are the values extracted from a URL based on a given route template.
            // values are sent in the "body" of the request. 
            // uri will be in HTTPS because we convert it in App.useHttpsRedirect
            // we have to specify the resource location in POST, how to access it with GET
            // 201 for post success, CreateAtRoute (the resource location(the GET uri to access the created resource), route Value, data)       
        }

        //PUT api/commands/{id}
        [HttpPut("{id}")]
        public ActionResult UpdateCommand(int id, CommandUpdateDto commandUpdateDto)
        {
            var commandModel = _repository.GetCommandById(id);
            if(commandModel == null)
            {
                return NotFound();
            }
            _mapper.Map(commandUpdateDto, commandModel); // Source, Destination
            // transefer from source to Command model. normally, we copy data from incoming object to new model object but here we transfer it to a existing model object. 
            //the model object is synced with db so only Savechanges is fine

            _repository.UpdateCommand(commandModel); 

            _repository.SaveChanges();

            return NoContent(); 
        }

         //PATCH api/commands/{id}
        [HttpPatch("{id}")]
        public ActionResult PartialCommandUpdate(int id, JsonPatchDocument<CommandUpdateDto> patchDoc)
        {
          // here we are getting a JSON patch or instruction manual for what to do on a json. 
          // JSON Patch is a format for expressing a sequence of operations to apply to a target JSON document
          //This format is also potentially useful in other cases in which it is necessary to make partial updates to a JSON document or to a data structure that has similar constraints   
            var commandModelFromRepo = _repository.GetCommandById(id);
            if(commandModelFromRepo == null)
            {
                return NotFound();
            }

            var commandToPatch = _mapper.Map<CommandUpdateDto>(commandModelFromRepo);
            patchDoc.ApplyTo(commandToPatch, ModelState); // patchDoc - format CommandToPatch using Json Patch and store it in Model State
            
            // The ModelState has two purposes: to store the value submitted to the server, and to store the validation errors associated with those values.
            // VALIDATIONS are the data annotaions used for models. 

            if(!TryValidateModel(commandToPatch)) // check for valudation error in the modified Model
            {
                return ValidationProblem(ModelState); // return the validation errors
            }

            _mapper.Map(commandToPatch, commandModelFromRepo);

            _repository.UpdateCommand(commandModelFromRepo);

            _repository.SaveChanges();

            return NoContent();
        }

          //DELETE api/commands/{id}
        [HttpDelete("{id}")]
        public ActionResult DeleteCommand(int id)
        {
            var commandModelFromRepo = _repository.GetCommandById(id);
            if(commandModelFromRepo == null)
            {
                return NotFound();
            }
            _repository.DeleteCommand(commandModelFromRepo);
            _repository.SaveChanges();

            return NoContent();
        }
    }
}