﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.Test.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DefaultRazorProjectServiceTest : TestBase
    {
        [Fact]
        public void UpdateProject_SameConfigurationNoops()
        {
            // Arrange
            var projectFilePath = "/C:/path/to/project.csproj";
            var ownerProject = TestProjectSnapshot.Create(projectFilePath);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.GetLoadedProject(projectFilePath))
                .Returns(ownerProject);
            projectSnapshotManager.Setup(manager => manager.HostProjectChanged(It.IsAny<HostProject>()))
                .Throws(new XunitException("Should not have been called."));
            var projectService = CreateProjectService(Mock.Of<ProjectResolver>(), projectSnapshotManager.Object);

            // Act & Assert
            projectService.UpdateProject(projectFilePath, ownerProject.Configuration);
        }

        [Fact]
        public void UpdateProject_NullConfigurationUsesDefault()
        {
            // Arrange
            var projectFilePath = "/C:/path/to/project.csproj";
            var ownerProject = TestProjectSnapshot.Create(projectFilePath);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.GetLoadedProject(projectFilePath))
                .Returns(ownerProject);
            projectSnapshotManager.Setup(manager => manager.HostProjectChanged(It.IsAny<HostProject>()))
                .Callback<HostProject>((hostProject) =>
                {
                    Assert.Same(RazorDefaults.Configuration, hostProject.Configuration);
                    Assert.Equal(projectFilePath, hostProject.FilePath);
                });
            var projectService = CreateProjectService(Mock.Of<ProjectResolver>(), projectSnapshotManager.Object);

            // Act
            projectService.UpdateProject(projectFilePath, configuration: null);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void UpdateProject_ChangesProjectToUseProvidedConfiguration()
        {
            // Arrange
            var projectFilePath = "/C:/path/to/project.csproj";
            var ownerProject = TestProjectSnapshot.Create(projectFilePath);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.GetLoadedProject(projectFilePath))
                .Returns(ownerProject);
            projectSnapshotManager.Setup(manager => manager.HostProjectChanged(It.IsAny<HostProject>()))
                .Callback<HostProject>((hostProject) =>
                {
                    Assert.Same(FallbackRazorConfiguration.MVC_1_1, hostProject.Configuration);
                    Assert.Equal(projectFilePath, hostProject.FilePath);
                });
            var projectService = CreateProjectService(Mock.Of<ProjectResolver>(), projectSnapshotManager.Object);

            // Act
            projectService.UpdateProject(projectFilePath, FallbackRazorConfiguration.MVC_1_1);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void UpdateProject_UntrackedProjectNoops()
        {
            // Arrange
            var projectFilePath = "/C:/path/to/project.csproj";
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.GetLoadedProject(projectFilePath))
                .Returns<ProjectSnapshot>(null);
            projectSnapshotManager.Setup(manager => manager.HostProjectChanged(It.IsAny<HostProject>()))
                .Throws(new XunitException("Should not have been called."));
            var projectService = CreateProjectService(Mock.Of<ProjectResolver>(), projectSnapshotManager.Object);

            // Act & Assert
            projectService.UpdateProject(projectFilePath, FallbackRazorConfiguration.MVC_1_1);
        }

        [Fact]
        public void TryUpdateViewImportDependencies_OnlyUpdatesOpenDocuments()
        {
            // Arrange
            var importFilePath = "/C:/path/to/_ViewImports.cshtml";
            var documentFilePath = "/C:/path/to/Index.cshtml";
            var closedDocumentFilePath = "/C:/path/to/About.cshtml";
            var ownerProject = TestProjectSnapshot.Create(
                "/C:/path/to/project.csproj",
                new[]
                {
                    importFilePath,
                    documentFilePath,
                    closedDocumentFilePath,
                });
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentChanged(ownerProject.FilePath, documentFilePath, It.IsAny<TextLoader>()));
            projectSnapshotManager.Setup(manager => manager.IsDocumentOpen(documentFilePath)).Returns(true);
            projectSnapshotManager.Setup(manager => manager.IsDocumentOpen(closedDocumentFilePath)).Returns(false);
            var projectService = CreateProjectService(Mock.Of<ProjectResolver>(), projectSnapshotManager.Object);

            // Act
            projectService.TryUpdateViewImportDependencies(importFilePath, ownerProject);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void TryUpdateViewImportDependencies_UpdatesOtherDocuments()
        {
            // Arrange
            var importFilePath = "/C:/path/to/_ViewImports.cshtml";
            var documentFilePath = "/C:/path/to/Index.cshtml";
            var ownerProject = TestProjectSnapshot.Create(
                "/C:/path/to/project.csproj",
                new[]
                {
                    importFilePath,
                    documentFilePath,
                });
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentChanged(ownerProject.FilePath, documentFilePath, It.IsAny<TextLoader>()));
            projectSnapshotManager.Setup(manager => manager.IsDocumentOpen(documentFilePath)).Returns(true);
            var projectService = CreateProjectService(Mock.Of<ProjectResolver>(), projectSnapshotManager.Object);

            // Act
            projectService.TryUpdateViewImportDependencies(importFilePath, ownerProject);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void TryUpdateViewImportDependencies_NoopsForNonViewImportFiles()
        {
            // Arrange
            var documentFilePath = "/C:/path/to/document.cshtml";
            var ownerProject = TestProjectSnapshot.Create("/C:/path/to/project.csproj");
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentChanged(ownerProject.FilePath, documentFilePath, It.IsAny<TextLoader>()))
                .Throws(new XunitException("Should not have been called."));
            var projectService = CreateProjectService(Mock.Of<ProjectResolver>(), projectSnapshotManager.Object);

            // Act & Assert
            projectService.TryUpdateViewImportDependencies(documentFilePath, ownerProject);
        }

        [Fact]
        public void CloseDocument_ClosesDocumentInOwnerProject()
        {
            // Arrange
            var expectedDocumentFilePath = "/C:/path/to/document.cshtml";
            var ownerProject = TestProjectSnapshot.Create("/C:/path/to/project.csproj");
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>
                {
                    [expectedDocumentFilePath] = ownerProject
                },
                TestProjectSnapshot.Create("/__MISC_PROJECT__"));
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentClosed(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TextLoader>()))
                .Callback<string, string, TextLoader>((projectFilePath, documentFilePath, text) =>
                {
                    Assert.Equal(ownerProject.HostProject.FilePath, projectFilePath);
                    Assert.Equal(expectedDocumentFilePath, documentFilePath);
                    Assert.NotNull(text);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);
            var textLoader = CreateTextLoader("Hello World");

            // Act
            projectService.CloseDocument(expectedDocumentFilePath, textLoader);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void CloseDocument_ClosesDocumentInMiscellaneousProject()
        {
            // Arrange
            var expectedDocumentFilePath = "/C:/path/to/document.cshtml";
            var miscellaneousProject = TestProjectSnapshot.Create("__MISC_PROJECT__");
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>(),
                miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentClosed(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TextLoader>()))
                .Callback<string, string, TextLoader>((projectFilePath, documentFilePath, text) =>
                {
                    Assert.Equal(miscellaneousProject.FilePath, projectFilePath);
                    Assert.Equal(expectedDocumentFilePath, documentFilePath);
                    Assert.NotNull(text);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);
            var textLoader = CreateTextLoader("Hello World");

            // Act
            projectService.CloseDocument(expectedDocumentFilePath, textLoader);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void OpenDocument_OpensAlreadyAddedDocumentInOwnerProject()
        {
            // Arrange
            var expectedDocumentFilePath = "/C:/path/to/document.cshtml";
            var ownerProject = TestProjectSnapshot.Create("/C:/path/to/project.csproj");
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>
                {
                    [expectedDocumentFilePath] = ownerProject
                },
                TestProjectSnapshot.Create("__MISC_PROJECT__"));
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProject>(), It.IsAny<HostDocument>(), It.IsAny<TextLoader>()))
                .Throws(new InvalidOperationException("This shouldn't have been called."));
            projectSnapshotManager.Setup(manager => manager.DocumentOpened(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SourceText>()))
                .Callback<string, string, SourceText>((projectFilePath, documentFilePath, text) =>
                {
                    Assert.Equal(ownerProject.HostProject.FilePath, projectFilePath);
                    Assert.Equal(expectedDocumentFilePath, documentFilePath);
                    Assert.NotNull(text);
                });
            var documentSnapshot = Mock.Of<DocumentSnapshot>();
            var documentResolver = Mock.Of<DocumentResolver>(resolver => resolver.TryResolveDocument(It.IsAny<string>(), out documentSnapshot) == true);
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object, documentResolver);
            var sourceText = SourceText.From("Hello World");

            // Act
            projectService.OpenDocument(expectedDocumentFilePath, sourceText, 1);

            // Assert
            projectSnapshotManager.Verify(manager => manager.DocumentOpened(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SourceText>()));
        }

        [Fact]
        public void OpenDocument_OpensAlreadyAddedDocumentInMiscellaneousProject()
        {
            // Arrange
            var expectedDocumentFilePath = "/C:/path/to/document.cshtml";
            var miscellaneousProject = TestProjectSnapshot.Create("__MISC_PROJECT__");
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>(),
                miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProject>(), It.IsAny<HostDocument>(), It.IsAny<TextLoader>()))
                .Throws(new InvalidOperationException("This shouldn't have been called."));
            projectSnapshotManager.Setup(manager => manager.DocumentOpened(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SourceText>()))
                .Callback<string, string, SourceText>((projectFilePath, documentFilePath, text) =>
                {
                    Assert.Equal(miscellaneousProject.FilePath, projectFilePath);
                    Assert.Equal(expectedDocumentFilePath, documentFilePath);
                    Assert.NotNull(text);
                });
            var documentSnapshot = Mock.Of<DocumentSnapshot>();
            var documentResolver = Mock.Of<DocumentResolver>(resolver => resolver.TryResolveDocument(It.IsAny<string>(), out documentSnapshot) == true);
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object, documentResolver);
            var sourceText = SourceText.From("Hello World");

            // Act
            projectService.OpenDocument(expectedDocumentFilePath, sourceText, 1);

            // Assert
            projectSnapshotManager.Verify(manager => manager.DocumentOpened(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SourceText>()));
        }

        [Fact]
        public void OpenDocument_OpensAndAddsDocumentToOwnerProject()
        {
            // Arrange
            var expectedDocumentFilePath = "/C:/path/to/document.cshtml";
            var ownerProject = TestProjectSnapshot.Create("/C:/path/to/project.csproj");
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>
                {
                    [expectedDocumentFilePath] = ownerProject
                },
                TestProjectSnapshot.Create("__MISC_PROJECT__"));
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProject>(), It.IsAny<HostDocument>(), It.IsAny<TextLoader>()))
                .Callback<HostProject, HostDocument, TextLoader>((hostProject, hostDocument, loader) =>
                {
                    Assert.Same(ownerProject.HostProject, hostProject);
                    Assert.Equal(expectedDocumentFilePath, hostDocument.FilePath);
                    Assert.Null(loader);
                });
            projectSnapshotManager.Setup(manager => manager.DocumentOpened(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SourceText>()))
                .Callback<string, string, SourceText>((projectFilePath, documentFilePath, text) =>
                {
                    Assert.Equal(ownerProject.HostProject.FilePath, projectFilePath);
                    Assert.Equal(expectedDocumentFilePath, documentFilePath);
                    Assert.NotNull(text);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);
            var sourceText = SourceText.From("Hello World");

            // Act
            projectService.OpenDocument(expectedDocumentFilePath, sourceText, 1);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void AddDocument_NoopsIfDocumentIsAlreadyAdded()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var project = Mock.Of<ProjectSnapshot>();
            var projectResolver = new Mock<ProjectResolver>();
            projectResolver.Setup(resolver => resolver.TryResolvePotentialProject(It.IsAny<string>(), out project))
                .Throws(new InvalidOperationException("This shouldn't have been called."));
            var alreadyOpenDoc = Mock.Of<DocumentSnapshot>();
            var documentResolver = Mock.Of<DocumentResolver>(resolver => resolver.TryResolveDocument(It.IsAny<string>(), out alreadyOpenDoc));
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProject>(), It.IsAny<HostDocument>(), It.IsAny<TextLoader>()))
                .Throws(new InvalidOperationException("This should not have been called."));
            var projectService = CreateProjectService(projectResolver.Object, projectSnapshotManager.Object, documentResolver);
            var textLoader = CreateTextLoader("Hello World");

            // Act & Assert
            projectService.AddDocument(documentFilePath, textLoader);
        }

        [Fact]
        public void AddDocument_AddsDocumentToOwnerProject()
        {
            // Arrange
            var documentFilePath = "/C:/path/to/document.cshtml";
            var ownerProject = TestProjectSnapshot.Create("/C:/path/to/project.csproj");
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>
                {
                    [documentFilePath] = ownerProject
                },
                TestProjectSnapshot.Create("__MISC_PROJECT__"));
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProject>(), It.IsAny<HostDocument>(), It.IsAny<TextLoader>()))
                .Callback<HostProject, HostDocument, TextLoader>((hostProject, hostDocument, loader) =>
                {
                    Assert.Same(ownerProject.HostProject, hostProject);
                    Assert.Equal(documentFilePath, hostDocument.FilePath);
                    Assert.NotNull(loader);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);
            var textLoader = CreateTextLoader("Hello World");

            // Act
            projectService.AddDocument(documentFilePath, textLoader);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void AddDocument_AddsDocumentToMiscellaneousProject()
        {
            // Arrange
            var documentFilePath = "/C:/path/to/document.cshtml";
            var miscellaneousProject = TestProjectSnapshot.Create("__MISC_PROJECT__");
            var projectResolver = new TestProjectResolver(new Dictionary<string, ProjectSnapshot>(), miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProject>(), It.IsAny<HostDocument>(), It.IsAny<TextLoader>()))
                .Callback<HostProject, HostDocument, TextLoader>((hostProject, hostDocument, loader) =>
                {
                    Assert.Same(miscellaneousProject.HostProject, hostProject);
                    Assert.Equal(documentFilePath, hostDocument.FilePath);
                    Assert.NotNull(loader);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);
            var textLoader = CreateTextLoader("Hello World");

            // Act
            projectService.AddDocument(documentFilePath, textLoader);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void RemoveDocument_RemovesDocumentFromOwnerProject()
        {
            // Arrange
            var documentFilePath = "/C:/path/to/document.cshtml";
            var ownerProject = TestProjectSnapshot.Create("/C:/path/to/project.csproj", new[] { documentFilePath });
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>
                {
                    [documentFilePath] = ownerProject
                },
                TestProjectSnapshot.Create("__MISC_PROJECT__"));
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentRemoved(It.IsAny<HostProject>(), It.IsAny<HostDocument>()))
                .Callback<HostProject, HostDocument>((hostProject, hostDocument) =>
                {
                    Assert.Same(ownerProject.HostProject, hostProject);
                    Assert.Equal(documentFilePath, hostDocument.FilePath);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.RemoveDocument(documentFilePath);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void RemoveDocument_RemovesDocumentFromMiscellaneousProject()
        {
            // Arrange
            var documentFilePath = "/C:/path/to/document.cshtml";
            var miscellaneousProject = TestProjectSnapshot.Create("__MISC_PROJECT__", new[] { documentFilePath });
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>(),
                miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentRemoved(It.IsAny<HostProject>(), It.IsAny<HostDocument>()))
                .Callback<HostProject, HostDocument>((hostProject, hostDocument) =>
                {
                    Assert.Same(miscellaneousProject.HostProject, hostProject);
                    Assert.Equal(documentFilePath, hostDocument.FilePath);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.RemoveDocument(documentFilePath);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void RemoveDocument_NoopsIfOwnerProjectDoesNotContainDocument()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var ownerProject = TestProjectSnapshot.Create("C:/path/to/project.csproj", new string[0]);
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>
                {
                    [documentFilePath] = ownerProject
                },
                TestProjectSnapshot.Create("__MISC_PROJECT__"));
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>();
            projectSnapshotManager.Setup(manager => manager.DocumentRemoved(It.IsAny<HostProject>(), It.IsAny<HostDocument>()))
                .Throws(new InvalidOperationException("Should not have been called."));
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act & Assert
            projectService.RemoveDocument(documentFilePath);
        }

        [Fact]
        public void RemoveDocument_NoopsIfMiscellaneousProjectDoesNotContainDocument()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var miscellaneousProject = TestProjectSnapshot.Create("__MISC_PROJECT__", new string[0]);
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>(),
                miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>();
            projectSnapshotManager.Setup(manager => manager.DocumentRemoved(It.IsAny<HostProject>(), It.IsAny<HostDocument>()))
                .Throws(new InvalidOperationException("Should not have been called."));
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act & Assert
            projectService.RemoveDocument(documentFilePath);
        }

        [Fact]
        public void UpdateDocument_ChangesDocumentInOwnerProject()
        {
            // Arrange
            var documentFilePath = "/C:/path/to/document.cshtml";
            var ownerProject = TestProjectSnapshot.Create("/C:/path/to/project.csproj", new[] { documentFilePath });
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>
                {
                    [documentFilePath] = ownerProject
                },
                TestProjectSnapshot.Create("__MISC_PROJECT__"));
            var newText = SourceText.From("Something New");
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentChanged(ownerProject.FilePath, documentFilePath, newText))
                .Callback<string, string, SourceText>((projectPath, documentPath, sourceText) =>
                {
                    Assert.Equal(ownerProject.FilePath, projectPath);
                    Assert.Equal(documentFilePath, documentPath);
                    Assert.Same(newText, sourceText);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.UpdateDocument(documentFilePath, newText, 1337);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void UpdateDocument_ChangesDocumentInMiscProject()
        {
            // Arrange
            var documentFilePath = "/C:/path/to/document.cshtml";
            var miscellaneousProject = TestProjectSnapshot.Create("__MISC_PROJECT__", new[] { documentFilePath });
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>(),
                TestProjectSnapshot.Create("__MISC_PROJECT__"));
            var newText = SourceText.From("Something New");
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentChanged(miscellaneousProject.FilePath, documentFilePath, newText))
                .Callback<string, string, SourceText>((projectPath, documentPath, sourceText) =>
                {
                    Assert.Equal(miscellaneousProject.FilePath, projectPath);
                    Assert.Equal(documentFilePath, documentPath);
                    Assert.Same(newText, sourceText);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.UpdateDocument(documentFilePath, newText, 1337);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void UpdateDocument_TracksKnownDocumentVersion()
        {
            // Arrange
            var documentFilePath = "/C:/path/to/document.cshtml";
            var ownerProject = TestProjectSnapshot.Create("/C:/path/to/project.csproj", new[] { documentFilePath });
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>
                {
                    [documentFilePath] = ownerProject
                },
                TestProjectSnapshot.Create("__MISC_PROJECT__"));
            DocumentSnapshot documentSnapshot = TestDocumentSnapshot.Create(documentFilePath);
            var documentResolver = Mock.Of<DocumentResolver>(resolver => resolver.TryResolveDocument(It.IsAny<string>(), out documentSnapshot) == true);
            var documentVersionCache = new Mock<DocumentVersionCache>(MockBehavior.Strict);
            documentVersionCache.Setup(cache => cache.TrackDocumentVersion(documentSnapshot, It.IsAny<long>()))
                .Callback<DocumentSnapshot, long>((snapshot, version) =>
                {
                    Assert.Same(documentSnapshot, snapshot);
                    Assert.Equal(1337, version);
                });
            var newText = SourceText.From("Something New");
            var projectService = CreateProjectService(
                projectResolver,
                Mock.Of<ProjectSnapshotManagerBase>(),
                documentResolver,
                documentVersionCache.Object);

            // Act
            projectService.UpdateDocument(documentFilePath, newText, 1337);

            // Assert
            documentVersionCache.VerifyAll();
        }

        [Fact]
        public void UpdateDocument_IgnoresUnknownDocumentVersions()
        {
            // Arrange
            var documentFilePath = "/C:/path/to/document.cshtml";
            var ownerProject = TestProjectSnapshot.Create("/C:/path/to/project.csproj", new[] { documentFilePath });
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>
                {
                    [documentFilePath] = ownerProject
                },
                TestProjectSnapshot.Create("__MISC_PROJECT__"));
            var documentVersionCache = new Mock<DocumentVersionCache>();
            documentVersionCache.Setup(cache => cache.TrackDocumentVersion(It.IsAny<DocumentSnapshot>(), It.IsAny<long>()))
                .Throws<XunitException>();
            var newText = SourceText.From("Something New");
            var projectService = CreateProjectService(
                projectResolver,
                Mock.Of<ProjectSnapshotManagerBase>(),
                documentVersionCache: documentVersionCache.Object);

            // Act & Assert
            projectService.UpdateDocument(documentFilePath, newText, 1337);
        }

        [Fact]
        public void AddProject_AddsProjectWithDefaultConfiguration()
        {
            // Arrange
            var projectFilePath = "/C:/path/to/project.csproj";
            var miscellaneousProject = TestProjectSnapshot.Create("__MISC_PROJECT__");
            var projectResolver = new TestProjectResolver(new Dictionary<string, ProjectSnapshot>(), miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.HostProjectAdded(It.IsAny<HostProject>()))
                .Callback<HostProject>((hostProject) =>
                {
                    Assert.Equal(projectFilePath, hostProject.FilePath);
                    Assert.Same(RazorDefaults.Configuration, hostProject.Configuration);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.AddProject(projectFilePath);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void RemoveProject_RemovesProject()
        {
            // Arrange
            var projectFilePath = "/C:/path/to/project.csproj";
            var ownerProject = TestProjectSnapshot.Create(projectFilePath);
            var miscellaneousProject = TestProjectSnapshot.Create("__MISC_PROJECT__");
            var projectResolver = new TestProjectResolver(new Dictionary<string, ProjectSnapshot>(), miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.GetLoadedProject(projectFilePath))
                .Returns(ownerProject);
            projectSnapshotManager.Setup(manager => manager.HostProjectRemoved(ownerProject.HostProject))
                .Callback<HostProject>((hostProject) =>
                {
                    Assert.Equal(projectFilePath, hostProject.FilePath);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.RemoveProject(projectFilePath);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void RemoveProject_NoopsIfProjectIsNotLoaded()
        {
            // Arrange
            var projectFilePath = "C:/path/to/project.csproj";
            var miscellaneousProject = TestProjectSnapshot.Create("__MISC_PROJECT__");
            var projectResolver = new TestProjectResolver(new Dictionary<string, ProjectSnapshot>(), miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>();
            projectSnapshotManager.Setup(manager => manager.HostProjectRemoved(It.IsAny<HostProject>()))
                .Throws(new InvalidOperationException("Should not have been called."));
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act & Assert
            projectService.RemoveProject(projectFilePath);
        }

        [Fact]
        public void TryMigrateDocumentsFromRemovedProject_MigratesDocumentsToNonMiscProject()
        {
            // Arrange
            var documentFilePath1 = "C:/path/to/document1.cshtml";
            var documentFilePath2 = "C:/path/to/document2.cshtml";
            var miscellaneousProject = TestProjectSnapshot.Create("__MISC_PROJECT__");
            var removedProject = TestProjectSnapshot.Create("C:/path/to/some/project.csproj", new[] { documentFilePath1, documentFilePath2 });
            var projectToBeMigratedTo = TestProjectSnapshot.Create("C:/path/to/project.csproj");
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>
                {
                    [documentFilePath1] = projectToBeMigratedTo,
                    [documentFilePath2] = projectToBeMigratedTo,
                },
                miscellaneousProject);
            var migratedDocuments = new List<HostDocument>();
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProject>(), It.IsAny<HostDocument>(), It.IsAny<TextLoader>()))
                .Callback<HostProject, HostDocument, TextLoader>((hostProject, hostDocument, textLoader) =>
                {
                    Assert.Same(projectToBeMigratedTo.HostProject, hostProject);
                    Assert.NotNull(textLoader);

                    migratedDocuments.Add(hostDocument);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.TryMigrateDocumentsFromRemovedProject(removedProject);

            // Assert
            Assert.Collection(migratedDocuments,
                document => Assert.Equal(documentFilePath1, document.FilePath),
                document => Assert.Equal(documentFilePath2, document.FilePath));
        }

        [Fact]
        public void TryMigrateDocumentsFromRemovedProject_MigratesDocumentsToMiscProject()
        {
            // Arrange
            var documentFilePath1 = "C:/path/to/document1.cshtml";
            var documentFilePath2 = "C:/path/to/document2.cshtml";
            var miscellaneousProject = TestProjectSnapshot.Create("__MISC_PROJECT__");
            var removedProject = TestProjectSnapshot.Create("C:/path/to/some/project.csproj", new[] { documentFilePath1, documentFilePath2 });
            var projectResolver = new TestProjectResolver(new Dictionary<string, ProjectSnapshot>(), miscellaneousProject);
            var migratedDocuments = new List<HostDocument>();
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProject>(), It.IsAny<HostDocument>(), It.IsAny<TextLoader>()))
                .Callback<HostProject, HostDocument, TextLoader>((hostProject, hostDocument, textLoader) =>
                {
                    Assert.Same(miscellaneousProject.HostProject, hostProject);
                    Assert.NotNull(textLoader);

                    migratedDocuments.Add(hostDocument);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.TryMigrateDocumentsFromRemovedProject(removedProject);

            // Assert
            Assert.Collection(migratedDocuments,
                document => Assert.Equal(documentFilePath1, document.FilePath),
                document => Assert.Equal(documentFilePath2, document.FilePath));
        }

        [Fact]
        public void TryMigrateMiscellaneousDocumentsToProject_DoesNotMigrateDocumentsIfNoOwnerProject()
        {
            // Arrange
            var documentFilePath1 = "C:/path/to/document1.cshtml";
            var documentFilePath2 = "C:/path/to/document2.cshtml";
            var miscellaneousProject = TestProjectSnapshot.Create("__MISC_PROJECT__", new[] { documentFilePath1, documentFilePath2 });
            var projectResolver = new TestProjectResolver(new Dictionary<string, ProjectSnapshot>(), miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>();
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProject>(), It.IsAny<HostDocument>(), It.IsAny<TextLoader>()))
                .Throws(new InvalidOperationException("Should not have been called."));
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act & Assert
            projectService.TryMigrateMiscellaneousDocumentsToProject();
        }

        [Fact]
        public void TryMigrateMiscellaneousDocumentsToProject_MigratesDocumentsToNewOwnerProject()
        {
            // Arrange
            var documentFilePath1 = "C:/path/to/document1.cshtml";
            var documentFilePath2 = "C:/path/to/document2.cshtml";
            var miscellaneousProject = TestProjectSnapshot.Create("__MISC_PROJECT__", new[] { documentFilePath1, documentFilePath2 });
            var projectToBeMigratedTo = TestProjectSnapshot.Create("C:/path/to/project.csproj");
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshot>
                {
                    [documentFilePath1] = projectToBeMigratedTo,
                    [documentFilePath2] = projectToBeMigratedTo,
                },
                miscellaneousProject);
            var migratedDocuments = new List<HostDocument>();
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProject>(), It.IsAny<HostDocument>(), It.IsAny<TextLoader>()))
                .Callback<HostProject, HostDocument, TextLoader>((hostProject, hostDocument, textLoader) =>
                {
                    Assert.Same(projectToBeMigratedTo.HostProject, hostProject);
                    Assert.NotNull(textLoader);

                    migratedDocuments.Add(hostDocument);
                });
            projectSnapshotManager.Setup(manager => manager.DocumentRemoved(It.IsAny<HostProject>(), It.IsAny<HostDocument>()))
                .Callback<HostProject, HostDocument>((hostProject, hostDocument) =>
                {
                    Assert.Same(miscellaneousProject.HostProject, hostProject);

                    Assert.DoesNotContain(hostDocument, migratedDocuments);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.TryMigrateMiscellaneousDocumentsToProject();

            // Assert
            Assert.Collection(migratedDocuments,
                document => Assert.Equal(documentFilePath1, document.FilePath),
                document => Assert.Equal(documentFilePath2, document.FilePath));
        }

        private static TextLoader CreateTextLoader(string content)
        {
            var sourceText = SourceText.From(content);
            var textAndVersion = TextAndVersion.Create(sourceText, VersionStamp.Default);
            var textLoader = TextLoader.From(textAndVersion);

            return textLoader;
        }

        private DefaultRazorProjectService CreateProjectService(
            ProjectResolver projectResolver,
            ProjectSnapshotManagerBase projectSnapshotManager,
            DocumentResolver documentResolver = null,
            DocumentVersionCache documentVersionCache = null)
        {
            documentVersionCache = documentVersionCache ?? Mock.Of<DocumentVersionCache>();
            var filePathNormalizer = new FilePathNormalizer();
            var accessor = Mock.Of<ProjectSnapshotManagerAccessor>(a => a.Instance == projectSnapshotManager);
            documentResolver = documentResolver ?? Mock.Of<DocumentResolver>();
            var hostDocumentFactory = new TestHostDocumentFactory();
            var projectService = new DefaultRazorProjectService(
                Dispatcher,
                hostDocumentFactory,
                documentResolver,
                projectResolver,
                documentVersionCache,
                filePathNormalizer,
                accessor,
                LoggerFactory);

            return projectService;
        }

        private class TestProjectResolver : ProjectResolver
        {
            private readonly IReadOnlyDictionary<string, ProjectSnapshot> _projectMappings;
            private readonly ProjectSnapshot _miscellaneousProject;

            public TestProjectResolver(IReadOnlyDictionary<string, ProjectSnapshot> projectMappings, ProjectSnapshot miscellaneousProject)
            {
                _projectMappings = projectMappings;
                _miscellaneousProject = miscellaneousProject;
            }

            public override ProjectSnapshot GetMiscellaneousProject() => _miscellaneousProject;

            public override bool TryResolvePotentialProject(string documentFilePath, out ProjectSnapshot projectSnapshot)
            {
                return _projectMappings.TryGetValue(documentFilePath, out projectSnapshot);
            }
        }

        private class TestHostDocumentFactory : HostDocumentFactory
        {
            public override HostDocument Create(string documentFilePath)
            {
                return new HostDocument(documentFilePath, documentFilePath);
            }
        }
    }
}
