using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTO.Price.Shared;
using CTO.Price.Shared.Domain;
using FluentAssertions;
using Grpc.Core;
using LanguageExt;
using Moq;
using Xunit;

namespace SharedTests
{
    public class DocumentHelperTest
    {

        #region TryAdd
        
        [Fact]
        public async Task TryAddNew_CreationSuccessful_ReturnUpdated()
        {
            var result = await DocumentHelper.TryAddNew<Unit>(() => Task.FromResult(Unit.Default));
            result.Should().Be(UpdateState.Updated);
        }
        
        [Fact]
        public async Task TryAddNew_CreationFailPriceServiceException_ReturnNeedRetry()
        {
            Func<Task> act = async () =>
            {
                await DocumentHelper.TryAddNew<Unit>(() => throw new PriceServiceException(PriceErrorCategory.UpdateFailed, "Known"));
            };
            await act.Should().ThrowAsync<PriceServiceException>();
        }
        [Fact]
        public async Task TryAddNew_CreationFailPriceServiceException_ReturnNeedRetry2()
        {
            Func<Task> act = async () =>
            {
                await DocumentHelper.TryAddNew<Unit>(() => Task.FromException<Unit>(new PriceServiceException(PriceErrorCategory.UpdateFailed, "Known")));
            };
            await act.Should().ThrowAsync<Exception>();
        }
        
        [Fact]
        public async Task TryAddNew_CreationFailUnexpectedException_ReturnNeedRetry()
        {
            Func<Task> act = async () =>
            {
                await DocumentHelper.TryAddNew<Unit>(() => throw new Exception("Unknown"));
            };
            await act.Should().ThrowAsync<Exception>();
        }
        
        [Fact]
        public async Task TryAddNew_CreationFailUnexpectedException_ReturnNeedRetry2()
        {
            Func<Task> act = async () =>
            {
                await DocumentHelper.TryAddNew<Unit>(() => Task.FromException<Unit>(new Exception("Unknown")));
            };
            await act.Should().ThrowAsync<Exception>();
        }
        
        #endregion
        
        #region TryUpdate
        
        [Fact]
        public async Task TryUpdate_UpdateSuccessful_ReturnUpdated()
        {
            var result = await DocumentHelper.TryUpdate<Unit>(() => Task.FromResult(Unit.Default));
            result.Should().Be(UpdateState.Updated);
        }
        
        [Fact]
        public async Task TryUpdate_UpdateFailPriceServiceException_ReturnNeedRetry()
        {
            Func<Task> act = async () =>
            {
                await DocumentHelper.TryUpdate<Unit>(() => throw new PriceServiceException(PriceErrorCategory.UpdateFailed, "Known"));
            };
            await act.Should().ThrowAsync<PriceServiceException>();
        }
        
        [Fact]
        public async Task TryUpdate_UpdateFailUnexpectedException_ReturnNeedRetry()
        {
            Func<Task> act = async () =>
            {
                await DocumentHelper.TryUpdate<Unit>(() => throw new Exception("Unknown"));
            };
            await act.Should().ThrowAsync<Exception>();
        }
        
        #endregion
        
        #region TryDelete
        
        [Fact]
        public async Task TryDelete_DeleteSuccessful_ReturnUpdated()
        {
            var result = await DocumentHelper.TryDelete<Unit>(() => Task.FromResult(Unit.Default));
            result.Should().Be(UpdateState.Deleted);
        }
        
        [Fact]
        public async Task TryDelete_DeleteFailPriceServiceException_ReturnNeedRetry()
        {
            Func<Task> act = async () =>
            {
                await DocumentHelper.TryDelete<Unit>(() => throw new PriceServiceException(PriceErrorCategory.UpdateFailed, "Known"));
            };
            await act.Should().ThrowAsync<PriceServiceException>();
        }
        
        [Fact]
        public async Task TryDelete_DeleteFailUnexpectedException_ReturnNeedRetry()
        {
            Func<Task> act = async () =>
            {
                await DocumentHelper.TryDelete<Unit>(() => throw new Exception("Unknown"));
            };
            await act.Should().ThrowAsync<Exception>();
        }
        
        #endregion
    }
}