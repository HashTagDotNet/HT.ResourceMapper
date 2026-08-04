using ResourceMapper.Common.Shared.Editor;
using ResourceMapper.Common.Shared.Editor.Contracts;

// ReSharper disable InconsistentNaming

namespace ResourceMapper.Common.Shared.Tests.Editor
{
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceMapper")]
    [Trait("Category", "ResourceMapper/Common")]
    [Trait("Category", "ResourceMapper/Common/Shared")]
    [Trait("Category", "ResourceMapper/Common/Shared/Editor")]
    [Trait("Category", "ResourceMapper/Common/Shared/Editor/EditorNavStack")]
    public class EditorNavStackTests
    {
        [Fact]
        public void HasFrames_NewStack_False()
        {
            var stack = new EditorNavStack();

            stack.HasFrames.Should().BeFalse("because nothing has been pushed yet");
            stack.Depth.Should().Be(0, "because the stack starts empty");
        }

        [Fact]
        public void Push_SingleFrame_HasFramesTrueAndDepthOne()
        {
            var stack = new EditorNavStack();

            stack.Push(Frame("resources", "DependsOn", "Create"));

            stack.HasFrames.Should().BeTrue("because a frame was pushed");
            stack.Depth.Should().Be(1, "because exactly one frame is on the stack");
        }

        [Fact]
        public void Peek_DoesNotRemoveTheFrame()
        {
            var stack = new EditorNavStack();
            var frame = Frame("resources", "DependsOn", "Create");
            stack.Push(frame);

            var peeked = stack.Peek();

            peeked.Should().BeSameAs(frame, "because Peek returns the top frame");
            stack.Depth.Should().Be(1, "because Peek must not pop — only Pop should remove a frame");
        }

        [Fact]
        public void Pop_RemovesAndReturnsTheTopFrame_LastInFirstOut()
        {
            var stack = new EditorNavStack();
            var first = Frame("resources", "DependsOn", "Create");
            var second = Frame("resources?n=1", "DependentOn", "Create");
            stack.Push(first);
            stack.Push(second);

            var popped = stack.Pop();

            popped.Should().BeSameAs(second, "because Pop is LIFO — the most recently pushed frame comes off first");
            stack.Depth.Should().Be(1, "because popping removes exactly one frame");
            stack.Peek().Should().BeSameAs(first, "because the first-pushed frame remains after the second is popped");
        }

        [Fact]
        public void Clear_RemovesAllFrames()
        {
            var stack = new EditorNavStack();
            stack.Push(Frame("resources", "DependsOn", "Create"));
            stack.Push(Frame("resources?n=1", "DependentOn", "Create"));

            stack.Clear();

            stack.HasFrames.Should().BeFalse("because Clear must abandon every frame, not just the top one");
            stack.Depth.Should().Be(0, "because Clear resets the depth to zero");
        }

        [Fact]
        public void EditorFrame_Returned_DefaultsToNull()
        {
            var frame = Frame("resources", "DependsOn", "Create");

            frame.Returned.Should().BeNull("because a freshly pushed frame has no returned child yet");
        }

        [Fact]
        public void EditorFrame_Returned_RoundTripsAReturnedChild()
        {
            var frame = Frame("resources", "DependsOn", "Create");
            var child = new ReturnedChild
            {
                ResourceUid = "child-uid",
                Name = "Child Name",
                TypeName = "E2eDepType",
                Domain = "non-prod"
            };

            frame.Returned = child;

            frame.Returned.Should().BeSameAs(child, "because the child set by the nested editor must be readable back on pop");
            frame.Returned!.ResourceUid.Should().Be("child-uid", "because the resource uid identifies the linked child");
            frame.Returned!.Domain.Should().Be("non-prod", "because the domain is carried through for the linked row's display");
        }

        [Fact]
        public void EditorFrame_CarriesParentRouteDirectionAndMode()
        {
            var frame = Frame("resources/parent-uid", "DependentOn", "Edit");

            frame.ParentRoute.Should().Be("resources/parent-uid", "because the parent route is used to detect ascent");
            frame.Direction.Should().Be("DependentOn", "because linking a returned child needs to know which tab it came from");
            frame.ParentMode.Should().Be("Edit", "because restoring a frame must resume the parent's mode (Create vs Edit)");
        }

        [Fact]
        public void EditorFrame_CarriesParentTab()
        {
            var frame = Frame("resources/parent-uid", "DependsOn", "Edit", parentTab: 2);

            frame.ParentTab.Should().Be(2, "because ascending must return to the tab whose picker started the nested create, not to General");
        }

        private static EditorFrame Frame(string parentRoute, string direction, string parentMode, int parentTab = 0) => new()
        {
            Response = new OpenEditorResponse(),
            ParentRoute = parentRoute,
            Direction = direction,
            ParentMode = parentMode,
            ParentTab = parentTab
        };
    }
}
