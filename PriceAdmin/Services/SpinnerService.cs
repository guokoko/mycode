using System;
using System.Diagnostics.CodeAnalysis;

namespace CTO.Price.Admin.Services
{
    [ExcludeFromCodeCoverageAttribute]
    public class SpinnerService
    {
        public event Action OnShow = default!;
        public event Action OnHide = default!;

        public void Show()
        {
            OnShow?.Invoke();
        }

        public void Hide()
        {
            OnHide?.Invoke();
        }
    }
}