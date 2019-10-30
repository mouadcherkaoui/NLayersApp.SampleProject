using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLayersApp.Controllers;
using NLayersApp.Controllers.Controllers;
using NLayersApp.Controllers.Tests.Fakes;
using NLayersApp.CQRS.Requests;
using NLayersApp.Helpers;
using NLayersApp.Persistence;
using NLayersApp.Persistence.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NLayersApp.SampleProject.Tests
{
    [TestClass]
    public class CommandControllerTests
    {
        List<TestModel> list = new List<TestModel>()
        {
            new TestModel(){ Id = 1, Name="test1", Description="description1", EmailAddress= "mail1@server.com"},
            new TestModel(){ Id = 2, Name="test2", Description="description2", EmailAddress= "mail2@server.com"},
            new TestModel(){ Id = 3, Name="test3", Description="description3",EmailAddress= "mail3@server.com"}
        };

        [TestInitialize]
        public void Initialize_Dependecy()
        {
            // dependencies registration
            IoC.Container.RegisterServices(s =>
            {                
                s.AddSingleton<ITypesResolver, TypesResolver>(s => new TypesResolver(() => new Type[] { typeof(TestModel) }));
                // fake create request handler
                s.AddScoped<IRequestHandler<CreateEntityRequest<TestModel>, TestModel>>(s => new FakeCreateEntityHandler<int, TestModel>(list));
                // fake read all request handler
                s.AddScoped<IRequestHandler<ReadEntitiesRequest<TestModel>, IEnumerable<TestModel>>>(s => new FakeReadEntitiesHandler<TestModel>(list));
                // fake read id request handler
                s.AddScoped<IRequestHandler<ReadEntityRequest<int, TestModel>, TestModel>>(s=> new FakeReadEntityHandler<int, TestModel>(list));
                // fake update request handler
                s.AddScoped<IRequestHandler<UpdateEntityRequest<int, TestModel>, TestModel>>(s => new FakeUpdateEntityHandler<int, TestModel>(list));
                // fake delete request handler
                s.AddScoped<IRequestHandler<DeleteEntityRequest<int, TestModel>, bool>>(s => new FakeDeleteEntityHandler<int, TestModel>(list));

                s.AddMediatR(Assembly.GetEntryAssembly());
            });
        }

        [TestMethod]
        public async Task Test_CommandController_For_TestModel()
        {
            var mediator = IoC.ServiceProvider.GetRequiredService<IMediator>();

            var controller = new CommandController<int, TestModel>(mediator);
            var current_result = await controller.Get();            

            Assert.AreEqual(list.Count, current_result.Count());
            Assert.AreEqual(list, current_result);
        }

        #region Get action tests

        [TestMethod]
        public async Task Test_Get_Action_OfType_TestModel()
        {
            var mediator = IoC.ServiceProvider.GetRequiredService<IMediator>();

            var controller = new CommandController<int, TestModel>(mediator);
            var current_result = await controller.Get();

            Assert.AreEqual(list.Count, current_result.Count());
            Assert.AreEqual(list, current_result);
        }


        [TestMethod]
        public async Task Test_Get_Id_Action_OfType_TestModel()
        {
            var mediator = IoC.ServiceProvider.GetRequiredService<IMediator>();

            var controller = new CommandController<int, TestModel>(mediator);
            var current_result = await controller.Get(1);

            Assert.AreEqual(list.FirstOrDefault(i => i.Id ==1), current_result);            
        }

        #endregion

        #region Post action tests

        [TestMethod]
        public async Task Test_Post_Id_Action_OfType_TestModel()
        {
            var mediator = IoC.ServiceProvider.GetRequiredService<IMediator>();

            var controller = new CommandController<int, TestModel>(mediator);
            var action_payload = new TestModel { Id = 5, Name = "inserted", Description = "inserted" };

            var current_result = await controller.Post(action_payload);
            
            // testing action return type
            Assert.IsInstanceOfType(current_result, typeof(OkObjectResult));
            
            // or simply 
            
            Assert.IsTrue(current_result is IActionResult);
            
            // testing returned value wrapped in an OkObjectResult
            Assert.AreEqual(action_payload, ((OkObjectResult)current_result).Value);
            
            // testing status code
            Assert.IsTrue(((OkObjectResult)current_result).StatusCode.Value == 201);
        }

        [TestMethod]
        public async Task Test_Post_Invalid_Model_Returns_BadRequestResult()
        {
            var mediator = IoC.ServiceProvider.GetRequiredService<IMediator>();

            var controller = new CommandController<int, TestModel>(mediator);

            var action_payload = new TestModel { Id = 5, Description = "inserted", EmailAddress = "" };

            controller.ModelState.AddModelError<TestModel>(t => t.Name, "name property is required!");
            controller.ModelState.AddModelError<TestModel>(t => t.EmailAddress, "email address is in an invalid format!");

            var current_result = await controller.Post(action_payload);

            var result_as_badRequestObjectResult = (BadRequestObjectResult)current_result;
            var objectResultType = result_as_badRequestObjectResult.Value.GetType();

            var result_payload = objectResultType.GetProperty("Payload").GetValue(result_as_badRequestObjectResult.Value);
            var result_errors = objectResultType.GetProperty("Errors").GetValue(result_as_badRequestObjectResult.Value);

            // testing action return type
            Assert.IsInstanceOfType(current_result, typeof(BadRequestObjectResult));

            // or simply 
            
            Assert.IsTrue(current_result is IActionResult);
            
            // testing returned value wrapped in an BadRequestObjectResult
            Assert.AreEqual(action_payload, result_payload);
            Assert.AreEqual(controller.ModelState.Values, result_errors);
            
            // testing status code
            Assert.IsTrue(result_as_badRequestObjectResult.StatusCode.Value == 400);
        }

        #endregion

        #region Put action tests 

        [TestMethod]
        public async Task Test_Put_Id_Action_OfType_TestModel()
        {
            var mediator = IoC.ServiceProvider.GetRequiredService<IMediator>();

            var controller = new CommandController<int, TestModel>(mediator);

            var action_payload = new TestModel { Id = 1, Name = "updated", Description = "updated" };
            var current_result = await controller.Put(1, action_payload);

            // testing action return type
            Assert.IsInstanceOfType(current_result, typeof(OkObjectResult));
            
            // or simply 
            
            Assert.IsTrue(current_result is IActionResult);
            
            // testing returned value wrapped in an OkObjectResult
            Assert.AreEqual(action_payload, ((OkObjectResult)current_result).Value);
            
            // testing status code
            Assert.IsTrue(((OkObjectResult)current_result).StatusCode == 200);
        }

        [TestMethod]
        public async Task Test_Put_Invalid_Model_Returns_BadRequestResult()
        {
            var mediator = IoC.ServiceProvider.GetRequiredService<IMediator>();

            var controller = new CommandController<int, TestModel>(mediator);

            var action_payload = new TestModel { Id = 1, Description = "updated", EmailAddress = "" };

            controller.ModelState.AddModelError<TestModel>(t => t.Name, "name property is required!");
            controller.ModelState.AddModelError<TestModel>(t => t.EmailAddress, "email address is in an invalid format!");

            var current_result = await controller.Put(1, action_payload);

            var result_as_badRequestObjectResult = (BadRequestObjectResult)current_result;
            var objectResultType = result_as_badRequestObjectResult.Value.GetType();

            var result_payload = objectResultType.GetProperty("Payload").GetValue(result_as_badRequestObjectResult.Value);
            var result_errors = objectResultType.GetProperty("Errors").GetValue(result_as_badRequestObjectResult.Value);

            // testing action return type
            Assert.IsInstanceOfType(current_result, typeof(BadRequestObjectResult));
            
            // or simply 
            
            Assert.IsTrue(current_result is IActionResult);
            
            // testing returned value wrapped in an BadRequestObjectResult
            Assert.AreEqual(action_payload, result_payload);
            Assert.AreEqual(controller.ModelState.Values, result_errors);
            
            // testing status code
            Assert.IsTrue(result_as_badRequestObjectResult.StatusCode.Value == 400);
        }

        #endregion

        #region Delete action tests

        [TestMethod]
        public async Task Test_Delete_Id_Action_OfType_TestModel()
        {
            var mediator = IoC.ServiceProvider.GetRequiredService<IMediator>();

            var controller = new CommandController<int, TestModel>(mediator);

            var action_payload = new TestModel { Id = 1};
            var current_result = await controller.Delete(action_payload.Id);
            var result_as_noContentResult = ((NoContentResult)current_result);

            // testing action return type
            Assert.IsInstanceOfType(current_result, typeof(NoContentResult));
            
            // or simply 
            
            Assert.IsTrue(current_result is IActionResult);
            
            // testing status code
            Assert.IsTrue(result_as_noContentResult.StatusCode == 204);
        }

        #endregion
    }
}
