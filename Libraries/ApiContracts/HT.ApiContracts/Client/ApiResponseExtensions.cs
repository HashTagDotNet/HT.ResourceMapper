using HT.Api.Contracts.Client.Interfaces;
using HT.Api.Contracts.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HT.Api.Contracts.Client
{
    public static class ApiResponseExtensions
    {
        /// <summary>
        /// Adds a validation error to the response.
        /// </summary>
        /// <param name="response">The API response.</param>
        /// <param name="propertyName">The name of the property that caused the validation error.</param>
        /// <param name="message">The validation error message.</param>
        public static void DoSomething(this ApiResponse response, string propertyName, string message)
        {
            
            response.AddValidation(propertyName, message);
            
        }
    }
}
