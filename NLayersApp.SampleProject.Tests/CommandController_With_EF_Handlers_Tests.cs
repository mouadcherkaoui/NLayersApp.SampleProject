using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLayersApp.Controllers;
using NLayersApp.Controllers.Controllers;
using NLayersApp.Controllers.Tests.Fakes;
using NLayersApp.CQRS.DependencyInjection;
using NLayersApp.CQRS.Requests;
using NLayersApp.Helpers;
using NLayersApp.Persistence;
using NLayersApp.Persistence.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NLayersApp.SampleProject.Tests
{
    [TestClass]
    public class CommandController_With_EF_Handlers_Tests
    {
        IMediator mediator;

        List<TestModel> test_Entities;
        public CommandController_With_EF_Handlers_Tests()
        {
            test_Entities = new List<TestModel>()
            {
                new TestModel(){ Name="test1", EmailAddress= "mail@server.com", Description="description1"},
                new TestModel(){ Name="test2", EmailAddress= "mail@server.com", Description="description2"},
                new TestModel(){ Name="test3", EmailAddress= "mail@server.com", Description="description3"}
            };
        }

        [TestInitialize]
        public async Task Initialize_Dependecy()
        {
            // dependencies registration
            IoC.Container.RegisterServices(s =>
            {
                var typesResolver = new TypesResolver(() => new Type[] { typeof(TestModel) });
                s.AddScoped<ITypesResolver>(s => typesResolver);
                s.AddDbContext<IContext, TDbContext<IdentityUser, IdentityRole, string>>(options => {
                    options.UseInMemoryDatabase("nlayersapp-db");
                    //.UseSqlite("Data Source=.\\Data\\nlayersapp.sqlite;");
                }, ServiceLifetime.Scoped);
                s.AddMediatRHandlers(typesResolver);
                s.AddMediatR(Assembly.GetEntryAssembly());
            });


            mediator = IoC.ServiceProvider.GetRequiredService<IMediator>();
            var context = IoC.ServiceProvider.GetRequiredService<IContext>();
            var context_as_dbContext = (DbContext)context;
            
            if (context_as_dbContext.Database.CanConnect())
                await context_as_dbContext.Database.EnsureDeletedAsync();
            await context_as_dbContext.Database.EnsureCreatedAsync();

            // migrate still not working on azure pipeline agent
            // ((DbContext)context).Database.Migrate();


            context.Set<TestModel>().AddRange(test_Entities);

            await context.SaveChangesAsync(CancellationToken.None);
        
        }

        [TestMethod]
        public async Task Test_CommandController_For_TestModel()
        {
            var controller = new CommandController<int, TestModel>(mediator);
            var current_result = await controller.Get();
            var current_result_values = current_result.ToList();

            Assert.AreEqual(test_Entities.Count, current_result.Count());

            foreach (var currentValue in current_result_values)
            {
                Assert.AreEqual(test_Entities.FirstOrDefault(i => i.Id == currentValue.Id), currentValue);
            }
        }

        [TestMethod]
        public async Task Test_Post_Id_Action_OfType_TestModel()
        {
            var controller = new CommandController<int, TestModel>(mediator);

            var action_payload = new TestModel { Name = "inserted", EmailAddress = "mail@server.com", Description = "inserted" };
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
        public async Task Test_Get_Action_OfType_TestModel()
        {
            var controller = new CommandController<int, TestModel>(mediator);
            var current_result = await controller.Get();
            var current_result_values = current_result.ToList();

            Assert.AreEqual(test_Entities.Count, current_result.Count());

            foreach (var currentValue in current_result_values)
            {
                Assert.AreEqual(test_Entities.FirstOrDefault(i => i.Id == currentValue.Id), currentValue);
            }
        }

        [TestMethod]
        public async Task Test_Get_Id_Action_OfType_TestModel()
        {
            var controller = new CommandController<int, TestModel>(mediator);
            var current_result = await controller.Get(1);

            Assert.AreEqual(test_Entities.FirstOrDefault(i => i.Id ==1), current_result);            
        }

        [TestMethod]
        public async Task Test_Put_Id_Action_OfType_TestModel()
        {
            var controller = new CommandController<int, TestModel>(mediator);

            var action_payload = new TestModel { Id = 1, Name = "updated", Description = "updated" };
            var current_result = await controller.Put(1, action_payload);
            var current_result_value = (TestModel)((OkObjectResult)current_result).Value;
            // testing action return type
            Assert.IsInstanceOfType(current_result, typeof(OkObjectResult));
            // or simply 
            Assert.IsTrue(current_result is IActionResult);
            // testing returned value wrapped in an OkObjectResult
            foreach (var property in typeof(TestModel).GetProperties(BindingFlags.Public))
            {
                Assert.AreEqual(property.GetValue(action_payload), property.GetValue(current_result_value));
            }
            //Assert.AreEqual(action_payload, current_result_value);
            // testing status code
            Assert.IsTrue(((OkObjectResult)current_result).StatusCode == 200);
        }



        [TestMethod]
        public async Task Test_Delete_Id_Action_OfType_TestModel()
        {
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
    }
}
