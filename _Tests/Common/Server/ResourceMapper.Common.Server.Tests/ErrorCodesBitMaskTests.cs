using HT.Api.Client.Contracts.Models;

namespace ResourceMapper.Common.Server.Tests
{
    public class ErrorCodesBitMaskTests
    {
        [Fact]
        public void ErrorCodes_BitMasks_ShouldHaveCorrectValues()
        {
            // Verify base values
            Assert.Equal(0, (int)ErrorCodes.Ok);
            Assert.Equal(1, (int)ErrorCodes.Cancelled);
            Assert.Equal(8, (int)ErrorCodes.Error);

            // Verify all error codes have expected bit patterns
            Assert.Equal(24, (int)ErrorCodes.InvalidArgument);   // 8 | 16
            Assert.Equal(40, (int)ErrorCodes.OperationTimeOut);  // 8 | 32
            Assert.Equal(72, (int)ErrorCodes.NotFound);          // 8 | 64
            Assert.Equal(136, (int)ErrorCodes.AlreadyExists);    // 8 | 128
            Assert.Equal(264, (int)ErrorCodes.PermissionDenied); // 8 | 256
            Assert.Equal(520, (int)ErrorCodes.ResourceExhausted); // 8 | 512
            Assert.Equal(1032, (int)ErrorCodes.FailedPrecondition); // 8 | 1024
            Assert.Equal(2056, (int)ErrorCodes.Aborted);         // 8 | 2048
            Assert.Equal(4104, (int)ErrorCodes.OutOfRange);      // 8 | 4096
            Assert.Equal(8200, (int)ErrorCodes.NotImplemented);  // 8 | 8192
            Assert.Equal(16392, (int)ErrorCodes.InternalError);  // 8 | 16384
            Assert.Equal(32776, (int)ErrorCodes.Unavailable);    // 8 | 32768
            Assert.Equal(65544, (int)ErrorCodes.Unauthenticated); // 8 | 65536
        }

        [Fact]
        public void ErrorCodes_VerifyBitMask_ShouldReturnTrueForAllErrorCodes()
        {
            // Test all error codes using the verification method
            foreach (ErrorCodes errorCode in Enum.GetValues<ErrorCodes>())
            {
                Assert.True(errorCode.VerifyBitMask(), 
                    $"Bit mask verification failed for {errorCode}: {errorCode.GetBitMaskAnalysis()}");
            }
        }

        [Fact]
        public void ErrorCodes_HasErrorFlag_ShouldWorkCorrectly()
        {
            // Success and cancelled should not have error flag
            Assert.False(ErrorCodes.Ok.HasErrorFlag());
            Assert.False(ErrorCodes.Cancelled.HasErrorFlag());

            // Base error should have error flag
            Assert.True(ErrorCodes.Error.HasErrorFlag());

            // All specific errors should have error flag
            Assert.True(ErrorCodes.InvalidArgument.HasErrorFlag());
            Assert.True(ErrorCodes.NotFound.HasErrorFlag());
            Assert.True(ErrorCodes.InternalError.HasErrorFlag());
            Assert.True(ErrorCodes.Unauthenticated.HasErrorFlag());
        }

        [Fact]
        public void ErrorCodes_IsSuccess_ShouldOnlyReturnTrueForOk()
        {
            Assert.True(ErrorCodes.Ok.IsSuccess());
            
            // All other values should not be success
            Assert.False(ErrorCodes.Cancelled.IsSuccess());
            Assert.False(ErrorCodes.Error.IsSuccess());
            Assert.False(ErrorCodes.InvalidArgument.IsSuccess());
            Assert.False(ErrorCodes.NotFound.IsSuccess());
        }

        [Fact]
        public void ErrorCodes_IsFailure_ShouldWorkCorrectly()
        {
            // Success and cancelled should not be failures
            Assert.False(ErrorCodes.Ok.IsFailure());
            Assert.False(ErrorCodes.Cancelled.IsFailure());

            // Base Error should not be considered a failure (it's a category)
            Assert.False(ErrorCodes.Error.IsFailure());

            // All specific errors should be failures
            Assert.True(ErrorCodes.InvalidArgument.IsFailure());
            Assert.True(ErrorCodes.NotFound.IsFailure());
            Assert.True(ErrorCodes.InternalError.IsFailure());
            Assert.True(ErrorCodes.Unauthenticated.IsFailure());
        }

        [Fact]
        public void ErrorCodes_GetSpecificErrorBits_ShouldReturnCorrectValues()
        {
            // Non-error codes should return 0
            Assert.Equal(0, ErrorCodes.Ok.GetSpecificErrorBits());
            Assert.Equal(0, ErrorCodes.Cancelled.GetSpecificErrorBits());

            // Base error should return 0 (no specific bits)
            Assert.Equal(0, ErrorCodes.Error.GetSpecificErrorBits());

            // Specific errors should return their unique bit patterns
            Assert.Equal(16, ErrorCodes.InvalidArgument.GetSpecificErrorBits());   // 2 << 3
            Assert.Equal(32, ErrorCodes.OperationTimeOut.GetSpecificErrorBits());  // 2 << 4
            Assert.Equal(64, ErrorCodes.NotFound.GetSpecificErrorBits());          // 2 << 5
            Assert.Equal(128, ErrorCodes.AlreadyExists.GetSpecificErrorBits());    // 2 << 6
        }

        [Fact]
        public void ErrorCodes_BitMaskAnalysis_ShouldProvideDetailedInformation()
        {
            var analysis = ErrorCodes.InvalidArgument.GetBitMaskAnalysis();
            
            Assert.Contains("InvalidArgument", analysis);
            Assert.Contains("Value: 24", analysis);
            Assert.Contains("HasErrorFlag: True", analysis);
            Assert.Contains("SpecificBits: 16", analysis);
        }

        [Theory]
        [InlineData(ErrorCodes.InvalidArgument, true)]
        [InlineData(ErrorCodes.NotFound, true)]
        [InlineData(ErrorCodes.PermissionDenied, true)]
        [InlineData(ErrorCodes.InternalError, false)]
        [InlineData(ErrorCodes.Unavailable, false)]
        public void ErrorCodes_IsClientError_ShouldClassifyCorrectly(ErrorCodes errorCode, bool expectedIsClient)
        {
            Assert.Equal(expectedIsClient, errorCode.IsClientError());
        }

        [Theory]
        [InlineData(ErrorCodes.InternalError, true)]
        [InlineData(ErrorCodes.Unavailable, true)]
        [InlineData(ErrorCodes.NotImplemented, true)]
        [InlineData(ErrorCodes.InvalidArgument, false)]
        [InlineData(ErrorCodes.NotFound, false)]
        public void ErrorCodes_IsServerError_ShouldClassifyCorrectly(ErrorCodes errorCode, bool expectedIsServer)
        {
            Assert.Equal(expectedIsServer, errorCode.IsServerError());
        }

        [Fact]
        public void ErrorCodes_AllValuesAreUnique_ShouldNotHaveDuplicates()
        {
            var errorCodeValues = Enum.GetValues<ErrorCodes>()
                .Select(ec => (int)ec)
                .ToList();

            var uniqueValues = errorCodeValues.Distinct().ToList();

            Assert.Equal(errorCodeValues.Count, uniqueValues.Count);
        }

        [Fact]
        public void ErrorCodes_ErrorFlagConsistency_ShouldBeConsistentAcrossAllMethods()
        {
            foreach (ErrorCodes errorCode in Enum.GetValues<ErrorCodes>())
            {
                var hasErrorFlag = errorCode.HasErrorFlag();
                var isFailure = errorCode.IsFailure();
                var isSuccess = errorCode.IsSuccess();
                var isCancelled = errorCode.IsCancelled();

                // Logical consistency checks
                if (isSuccess)
                {
                    Assert.False(hasErrorFlag, $"{errorCode} is success but has error flag");
                    Assert.False(isFailure, $"{errorCode} is success but is also failure");
                }

                if (isCancelled)
                {
                    Assert.False(hasErrorFlag, $"{errorCode} is cancelled but has error flag");
                    Assert.False(isFailure, $"{errorCode} is cancelled but is also failure");
                    Assert.False(isSuccess, $"{errorCode} is cancelled but is also success");
                }

                if (isFailure)
                {
                    Assert.True(hasErrorFlag, $"{errorCode} is failure but doesn't have error flag");
                    Assert.False(isSuccess, $"{errorCode} is failure but is also success");
                    Assert.False(isCancelled, $"{errorCode} is failure but is also cancelled");
                }
            }
        }
    }
}