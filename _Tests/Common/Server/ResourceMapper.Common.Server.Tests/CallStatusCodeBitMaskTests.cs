using HT.Api.Client.Contracts.Models;
// ReSharper disable InconsistentNaming

namespace ResourceMapper.Common.Server.Tests
{
    public class CallStatusCodeBitMaskTests
    {
        [Fact]
        public void CallStatusCode_BitMasks_ShouldHaveCorrectValues()
        {
            // Verify base values
            Assert.Equal(0, (int)CallStatusCode.Ok);
            Assert.Equal(1, (int)CallStatusCode.Cancelled);
            Assert.Equal(8, (int)CallStatusCode.Error);

            // Verify all status codes have expected bit patterns
            Assert.Equal(24, (int)CallStatusCode.InvalidArgument);   // 8 | 16
            Assert.Equal(40, (int)CallStatusCode.OperationTimeOut);  // 8 | 32
            Assert.Equal(72, (int)CallStatusCode.NotFound);          // 8 | 64
            Assert.Equal(136, (int)CallStatusCode.AlreadyExists);    // 8 | 128
            Assert.Equal(264, (int)CallStatusCode.PermissionDenied); // 8 | 256
            Assert.Equal(520, (int)CallStatusCode.ResourceExhausted); // 8 | 512
            Assert.Equal(1032, (int)CallStatusCode.FailedPrecondition); // 8 | 1024
            Assert.Equal(2056, (int)CallStatusCode.Aborted);         // 8 | 2048
            Assert.Equal(4104, (int)CallStatusCode.OutOfRange);      // 8 | 4096
            Assert.Equal(8200, (int)CallStatusCode.NotImplemented);  // 8 | 8192
            Assert.Equal(16392, (int)CallStatusCode.InternalError);  // 8 | 16384
            Assert.Equal(32776, (int)CallStatusCode.Unavailable);    // 8 | 32768
            Assert.Equal(65544, (int)CallStatusCode.Unauthenticated); // 8 | 65536
        }

        [Fact]
        public void CallStatusCode_VerifyBitMask_ShouldReturnTrueForAllCallStatusCodes()
        {
            // Test all status codes using the verification method
            foreach (CallStatusCode statusCode in Enum.GetValues<CallStatusCode>())
            {
                Assert.True(statusCode.VerifyBitMask(), 
                    $"Bit mask verification failed for {statusCode}: {statusCode.GetBitMaskAnalysis()}");
            }
        }

        [Fact]
        public void CallStatusCode_HasErrorFlag_ShouldWorkCorrectly()
        {
            // Success and cancelled should not have error flag
            Assert.False(CallStatusCode.Ok.HasErrorFlag());
            Assert.False(CallStatusCode.Cancelled.HasErrorFlag());

            // Base error should have error flag
            Assert.True(CallStatusCode.Error.HasErrorFlag());

            // All specific errors should have error flag
            Assert.True(CallStatusCode.InvalidArgument.HasErrorFlag());
            Assert.True(CallStatusCode.NotFound.HasErrorFlag());
            Assert.True(CallStatusCode.InternalError.HasErrorFlag());
            Assert.True(CallStatusCode.Unauthenticated.HasErrorFlag());
        }

        [Fact]
        public void CallStatusCode_IsSuccess_ShouldOnlyReturnTrueForOk()
        {
            Assert.True(CallStatusCode.Ok.IsSuccess());
            
            // All other values should not be success
            Assert.False(CallStatusCode.Cancelled.IsSuccess());
            Assert.False(CallStatusCode.Error.IsSuccess());
            Assert.False(CallStatusCode.InvalidArgument.IsSuccess());
            Assert.False(CallStatusCode.NotFound.IsSuccess());
        }

        [Fact]
        public void CallStatusCode_IsFailure_ShouldWorkCorrectly()
        {
            // Success and cancelled should not be failures
            Assert.False(CallStatusCode.Ok.IsFailure());
            Assert.False(CallStatusCode.Cancelled.IsFailure());

            // Base Error should not be considered a failure (it's a category)
            Assert.False(CallStatusCode.Error.IsFailure());

            // All specific errors should be failures
            Assert.True(CallStatusCode.InvalidArgument.IsFailure());
            Assert.True(CallStatusCode.NotFound.IsFailure());
            Assert.True(CallStatusCode.InternalError.IsFailure());
            Assert.True(CallStatusCode.Unauthenticated.IsFailure());
        }

        [Fact]
        public void CallStatusCode_GetSpecificErrorBits_ShouldReturnCorrectValues()
        {
            // Non-error codes should return 0
            Assert.Equal(0, CallStatusCode.Ok.GetSpecificErrorBits());
            Assert.Equal(0, CallStatusCode.Cancelled.GetSpecificErrorBits());

            // Base error should return 0 (no specific bits)
            Assert.Equal(0, CallStatusCode.Error.GetSpecificErrorBits());

            // Specific errors should return their unique bit patterns
            Assert.Equal(16, CallStatusCode.InvalidArgument.GetSpecificErrorBits());   // 2 << 3
            Assert.Equal(32, CallStatusCode.OperationTimeOut.GetSpecificErrorBits());  // 2 << 4
            Assert.Equal(64, CallStatusCode.NotFound.GetSpecificErrorBits());          // 2 << 5
            Assert.Equal(128, CallStatusCode.AlreadyExists.GetSpecificErrorBits());    // 2 << 6
        }

        [Fact]
        public void CallStatusCode_BitMaskAnalysis_ShouldProvideDetailedInformation()
        {
            var analysis = CallStatusCode.InvalidArgument.GetBitMaskAnalysis();
            
            Assert.Contains("InvalidArgument", analysis);
            Assert.Contains("Value: 24", analysis);
            Assert.Contains("HasErrorFlag: True", analysis);
            Assert.Contains("SpecificBits: 16", analysis);
        }

        [Theory]
        [InlineData(CallStatusCode.InvalidArgument, true)]
        [InlineData(CallStatusCode.NotFound, true)]
        [InlineData(CallStatusCode.PermissionDenied, true)]
        [InlineData(CallStatusCode.InternalError, false)]
        [InlineData(CallStatusCode.Unavailable, false)]
        public void CallStatusCode_IsClientError_ShouldClassifyCorrectly(CallStatusCode statusCode, bool expectedIsClient)
        {
            Assert.Equal(expectedIsClient, statusCode.IsClientError());
        }

        [Theory]
        [InlineData(CallStatusCode.InternalError, true)]
        [InlineData(CallStatusCode.Unavailable, true)]
        [InlineData(CallStatusCode.NotImplemented, true)]
        [InlineData(CallStatusCode.InvalidArgument, false)]
        [InlineData(CallStatusCode.NotFound, false)]
        public void CallStatusCode_IsServerError_ShouldClassifyCorrectly(CallStatusCode statusCode, bool expectedIsServer)
        {
            Assert.Equal(expectedIsServer, statusCode.IsServerError());
        }

        [Fact]
        public void CallStatusCode_AllValuesAreUnique_ShouldNotHaveDuplicates()
        {
            var statusCodeValues = Enum.GetValues<CallStatusCode>()
                .Select(sc => (int)sc)
                .ToList();

            var uniqueValues = statusCodeValues.Distinct().ToList();

            Assert.Equal(statusCodeValues.Count, uniqueValues.Count);
        }

        [Fact]
        public void CallStatusCode_ErrorFlagConsistency_ShouldBeConsistentAcrossAllMethods()
        {
            foreach (CallStatusCode statusCode in Enum.GetValues<CallStatusCode>())
            {
                var hasErrorFlag = statusCode.HasErrorFlag();
                var isFailure = statusCode.IsFailure();
                var isSuccess = statusCode.IsSuccess();
                var isCancelled = statusCode.IsCancelled();

                // Logical consistency checks
                if (isSuccess)
                {
                    Assert.False(hasErrorFlag, $"{statusCode} is success but has error flag");
                    Assert.False(isFailure, $"{statusCode} is success but is also failure");
                }

                if (isCancelled)
                {
                    Assert.False(hasErrorFlag, $"{statusCode} is cancelled but has error flag");
                    Assert.False(isFailure, $"{statusCode} is cancelled but is also failure");
                    Assert.False(isSuccess, $"{statusCode} is cancelled but is also success");
                }

                if (isFailure)
                {
                    Assert.True(hasErrorFlag, $"{statusCode} is failure but doesn't have error flag");
                    Assert.False(isSuccess, $"{statusCode} is failure but is also success");
                    Assert.False(isCancelled, $"{statusCode} is failure but is also cancelled");
                }
            }
        }
    }
}