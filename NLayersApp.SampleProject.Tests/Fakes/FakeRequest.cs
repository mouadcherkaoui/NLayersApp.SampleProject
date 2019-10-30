using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace NLayersApp.Controllers.Tests.Fakes
{
    public class FakeRequest<TRequest, TResult> : IRequest<TResult>
    {
        TRequest _request;
        public FakeRequest(TRequest request)
        {
            _request = request;
        }
    }
}
