namespace Undersoft.SDK.Service.Application.GUI.View.Abstraction
{

    public interface IViewFilter : IViewItem, IViewLoadable
    {
        bool IsOpen { get; set; }

        bool Added { get; }

        bool IsAddable { get; }

        void Close();

        void CloneLast();

        void RemoveLast();

        void Update();

        void Clear();

        Task ApplyAsync();
    }
}