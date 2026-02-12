using CAD2DModel.Commands;
using CAD2DModel.Commands.Implementations;
using CAD2DModel.Geometry;
using CAD2DModel.Services;
using FluentAssertions;
using Moq;

namespace CAD2DModel.Tests.Commands;

[TestClass]
public class CommandManagerTests
{
    private CommandManager _commandManager = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _commandManager = new CommandManager();
    }
    
    [TestMethod]
    public void Execute_ShouldExecuteCommand()
    {
        // Arrange
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.CanExecute()).Returns(true);
        mockCommand.Setup(c => c.Description).Returns("Test Command");
        
        // Act
        _commandManager.Execute(mockCommand.Object);
        
        // Assert
        mockCommand.Verify(c => c.Execute(), Times.Once);
        _commandManager.CanUndo.Should().BeTrue();
    }
    
    [TestMethod]
    public void Undo_ShouldUndoLastCommand()
    {
        // Arrange
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.CanExecute()).Returns(true);
        mockCommand.Setup(c => c.Description).Returns("Test Command");
        
        _commandManager.Execute(mockCommand.Object);
        
        // Act
        _commandManager.Undo();
        
        // Assert
        mockCommand.Verify(c => c.Undo(), Times.Once);
        _commandManager.CanUndo.Should().BeFalse();
        _commandManager.CanRedo.Should().BeTrue();
    }
    
    [TestMethod]
    public void Redo_ShouldRedoLastUndoneCommand()
    {
        // Arrange
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.CanExecute()).Returns(true);
        mockCommand.Setup(c => c.Description).Returns("Test Command");
        
        _commandManager.Execute(mockCommand.Object);
        _commandManager.Undo();
        
        // Act
        _commandManager.Redo();
        
        // Assert
        mockCommand.Verify(c => c.Execute(), Times.Exactly(2));
        _commandManager.CanUndo.Should().BeTrue();
        _commandManager.CanRedo.Should().BeFalse();
    }
    
    [TestMethod]
    public void Execute_AfterUndo_ShouldClearRedoStack()
    {
        // Arrange
        var mockCommand1 = new Mock<ICommand>();
        mockCommand1.Setup(c => c.CanExecute()).Returns(true);
        mockCommand1.Setup(c => c.Description).Returns("Command 1");
        
        var mockCommand2 = new Mock<ICommand>();
        mockCommand2.Setup(c => c.CanExecute()).Returns(true);
        mockCommand2.Setup(c => c.Description).Returns("Command 2");
        
        _commandManager.Execute(mockCommand1.Object);
        _commandManager.Undo();
        
        // Act
        _commandManager.Execute(mockCommand2.Object);
        
        // Assert
        _commandManager.CanRedo.Should().BeFalse();
    }
    
    [TestMethod]
    public void Clear_ShouldClearAllStacks()
    {
        // Arrange
        var mockCommand = new Mock<ICommand>();
        mockCommand.Setup(c => c.CanExecute()).Returns(true);
        mockCommand.Setup(c => c.Description).Returns("Test Command");
        
        _commandManager.Execute(mockCommand.Object);
        
        // Act
        _commandManager.Clear();
        
        // Assert
        _commandManager.CanUndo.Should().BeFalse();
        _commandManager.CanRedo.Should().BeFalse();
    }
}

[TestClass]
public class GeometryCommandsTests
{
    [TestMethod]
    public void AddVertexCommand_Execute_ShouldAddVertex()
    {
        // Arrange
        var polyline = new Polyline();
        var location = new Point2D(1, 2);
        var command = new AddVertexCommand(polyline, location);
        
        // Act
        command.Execute();
        
        // Assert
        polyline.VertexCount.Should().Be(1);
        polyline.Vertices[0].Location.Should().Be(location);
    }
    
    [TestMethod]
    public void AddVertexCommand_Undo_ShouldRemoveVertex()
    {
        // Arrange
        var polyline = new Polyline();
        var location = new Point2D(1, 2);
        var command = new AddVertexCommand(polyline, location);
        
        command.Execute();
        
        // Act
        command.Undo();
        
        // Assert
        polyline.VertexCount.Should().Be(0);
    }
    
    [TestMethod]
    public void MoveVertexCommand_Execute_ShouldMoveVertex()
    {
        // Arrange
        var vertex = new Vertex(new Point2D(0, 0));
        var newLocation = new Point2D(5, 5);
        var command = new MoveVertexCommand(vertex, newLocation);
        
        // Act
        command.Execute();
        
        // Assert
        vertex.Location.Should().Be(newLocation);
    }
    
    [TestMethod]
    public void MoveVertexCommand_Undo_ShouldRestoreOriginalLocation()
    {
        // Arrange
        var originalLocation = new Point2D(0, 0);
        var vertex = new Vertex(originalLocation);
        var newLocation = new Point2D(5, 5);
        var command = new MoveVertexCommand(vertex, newLocation);
        
        command.Execute();
        
        // Act
        command.Undo();
        
        // Assert
        vertex.Location.Should().Be(originalLocation);
    }
    
    [TestMethod]
    public void AddPolylineCommand_Execute_ShouldAddPolylineToModel()
    {
        // Arrange
        var mockModel = new Mock<IGeometryModel>();
        var polyline = new Polyline();
        var command = new AddPolylineCommand(mockModel.Object, polyline);
        
        // Act
        command.Execute();
        
        // Assert
        mockModel.Verify(m => m.AddEntity(polyline), Times.Once);
    }
}
