using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using HT.Api.Contracts.Client.Models;
using HT.Api.Contracts.Server.Response;
using HT.Services.Common.Server.Domains;
using HT.Services.Common.Shared.Domains.Contracts;
using HT.System.Text.Json;

namespace HT.Api.Contracts.Tests
{
    public class UnitTest1
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowOutOfOrderMetadataProperties = true,
            Converters =
            {
                new JsonStringEnumConverter()
            },
            IgnoreReadOnlyProperties = false,
            IgnoreReadOnlyFields = false,
            IncludeFields = false,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            PropertyNameCaseInsensitive = true,
           // ReferenceHandler = ReferenceHandler.Preserve,
            RespectNullableAnnotations = true,
            TypeInfoResolver = (new DefaultJsonTypeInfoResolver())
                .WithAddedModifier(JsonExtensions.AddShouldSerializeMethodsForTypeHierarchy<object>())
        };

        [Fact]
        public void Test1()
        {

            var response = new ApiServiceResponse<TestData>()
            {
                ApiResponse = new ApiResponse<TestData>()
                {
                    Data = new TestData("first", "last")
                }
            };

            response.AddMessage("message code", "message text");
            
            // act

            var serializedResponse = JsonSerializer.Serialize(response, _jsonSerializerOptions);

            // assert
            Assert.NotNull(serializedResponse);
        }
        [Fact]
        public void Test3()
        {
            //var serviceResponse = new ApiServiceResponse<TestData>()
            //{
            //    ApiResponse = new ApiResponse<TestData>()
            //    {
            //        Data = new TestData("first", "last")
            //    }
            //};

            var serviceResponse = new ApiServiceResponse();
           
            var serviceResponse2 = serviceResponse
                .SetHttpStatus(HttpStatusCode.Found, "Lucky Guess")
                .AsNotFound("your object is not found. Try again")
                .AddHeader("x-ht-correlation", "my corrleation id");
   
            // act

            var serializedResponse = JsonSerializer.Serialize(serviceResponse.ApiResponse, _jsonSerializerOptions);

            // assert
            Assert.NotNull(serializedResponse);
        }
        [Fact]
        public void Test2()
        {
            CreateDomainRequest domain;
            domain = new CreateDomainRequest()
            {
                Name = "na",
                Code = "code af asfa asf asfas asfd",
                Description = "description",
                LanguageCode = "en"
            };
            var validator = new CreateDomainRequestValidator();
            var validationResult = validator.Validate(domain);

            //var testMessage = new ApiFieldValidationMessage()
            //{
            //    FieldName = "fieldname",
            //    Message = "something is wrong with data",
            //};

            //var response = new HTServiceResponse<TestData>();
            //response.ApiResponse.Errors ??= new();
            //response.ApiResponse.Errors.Add(new ErrorMessage()
            //    {
            //        SourceCode = RequestSource.Body,
            //        Code = "code",
            //        StatusCode = ErrorCodes.Cancelled
            //    }
            //);


            //response.HttpStatusCode = HttpStatusCode.BadGateway;
            //response.AddValidationMessage(testMessage.FieldName,testMessage.Message);

            //// act
            //var serializedResponse = JsonSerializer.Serialize(response, _jsonSerializerOptions);

            //// assert
            //Assert.NotNull(serializedResponse);


        }

        private record class TestData
        {
            public TestData()
            {
                FirstName = null;
                LastName = null;
            }
            public TestData(string firstName, string lastName)
            {
                FirstName = firstName;
                LastName = lastName;
            }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
        }

    
    }
}
