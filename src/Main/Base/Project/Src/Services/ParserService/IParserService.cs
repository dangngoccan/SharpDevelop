﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ICSharpCode.Core;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Project;
using ICSharpCode.SharpDevelop.Refactoring;

namespace ICSharpCode.SharpDevelop.Parser
{
	/// <summary>
	/// Manages parse runs and caches ParseInformation.
	/// </summary>
	public interface IParserService
	{
		/// <summary>
		/// Gets/Sets the task list tokens.
		/// The getter of this property is thread-safe;
		/// the setter must only be called on the main thread.
		/// </summary>
		IReadOnlyList<string> TaskListTokens { get; set; }
		
		#region Load Solution Projects Thread
		/// <summary>
		/// Gets whether the solution is being loaded, or a major re-parse is happening
		/// (e.g. after adding a project).
		/// </summary>
		/// <remarks>This property is only changed by the main thread.</remarks>
		bool LoadSolutionProjectsThreadRunning { get; }
		
		/// <summary>
		/// This event is raised when the LoadSolutionProjectsThreadRunning property changes to <c>true</c>.
		/// This always happens on the main thread.
		/// </summary>
		event EventHandler LoadSolutionProjectsThreadStarted;
		
		/// <summary>
		/// This event is raised when the LoadSolutionProjectsThreadRunning property changes to <c>false</c>.
		/// This always happens on the main thread.
		/// </summary>
		event EventHandler LoadSolutionProjectsThreadEnded; // TODO: rename to finished
		#endregion
		
		#region GetCompilation
		/// <summary>
		/// Gets or creates a compilation for the specified project.
		/// </summary>
		/// <remarks>
		/// This method is thread-safe.
		/// This method never returns null - in case of errors, a dummy compilation is created.
		/// </remarks>
		ICompilation GetCompilation(IProject project);
		
		/// <summary>
		/// Gets or creates a compilation for the project that contains the specified file.
		/// </summary>
		/// <remarks>
		/// This method is thread-safe.
		/// This method never returns null - in case of errors, a dummy compilation is created.
		/// </remarks>
		ICompilation GetCompilationForFile(FileName fileName);
		
		/// <summary>
		/// Gets a snapshot of the current compilations
		/// This method is useful when a consistent snapshot across multiple compilations is needed.
		/// </summary>
		/// <remarks>
		/// This method is thread-safe.
		/// </remarks>
		SharpDevelopSolutionSnapshot GetCurrentSolutionSnapshot();
		
		/// <summary>
		/// Invalidates the current solution snapshot, causing
		/// the next <see cref="GetCurrentSolutionSnapshot()"/> call to create a new one.
		/// This method needs to be called whenever IProject.ProjectContent changes.
		/// </summary>
		/// <remarks>
		/// This method is thread-safe.
		/// </remarks>
		void InvalidateCurrentSolutionSnapshot();
		#endregion
		
		#region GetExistingParsedFile
		/// <summary>
		/// Gets the unresolved type system for the specified file.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="version">
		/// Optional: requested version of the file.
		/// If this parameter is specified and the existing parsed file belongs to a different version,
		/// this method will return null.
		/// </param>
		/// <param name="parentProject">
		/// Optional: If the file is part of multiple projects, specifies
		/// which parsed version of the file to return (for example, different project settings
		/// can cause the file to be parsed differently).
		/// </param>
		/// <returns>
		/// Returns the IParsedFile for the specified file,
		/// or null if the file has not been parsed yet.
		/// </returns>
		/// <remarks>This method is thread-safe.</remarks>
		IParsedFile GetExistingParsedFile(FileName fileName, ITextSourceVersion version = null, IProject parentProject = null);
		
		/// <summary>
		/// Gets full parse information for the specified file, if it is available.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="version">
		/// Optional: requested version of the file.
		/// If this parameter is specified and the existing parsed file belongs to a different version,
		/// this method will return null.
		/// </param>
		/// <param name="parentProject">
		/// Optional: If the file is part of multiple projects, specifies
		/// which parsed version of the file to return (for example, different project settings
		/// can cause the file to be parsed differently).
		/// </param>
		/// <returns>
		/// If only the IParsedFile is available (non-full parse information), this method returns null.
		/// </returns>
		ParseInformation GetCachedParseInformation(FileName fileName, ITextSourceVersion version = null, IProject parentProject = null);
		#endregion
		
		#region Parse
		/// <summary>
		/// Parses the specified file.
		/// Produces full parse information.
		/// </summary>
		/// <param name="fileName">Name of the file to parse</param>
		/// <param name="fileContent">Optional: Content of the file to parse.</param>
		/// <param name="parentProject">
		/// Optional: If the file is part of multiple projects, specifies
		/// which parsed version of the file to return (for example, different project settings
		/// can cause the file to be parsed differently).
		/// </param>
		/// <returns>
		/// Returns the ParseInformation for the specified file, or null if the file cannot be parsed.
		/// For files currently open in an editor, this method does not necessary reparse, but may return
		/// an existing cached parse information (but only if it's still up-to-date).
		/// </returns>
		/// <remarks>
		/// This method is thread-safe.
		/// <para>
		/// If <paramref name="fileContent"/> is null, this method will block and wait for the main thread
		/// to retrieve the latest file content. This can cause deadlocks if this method is called within a lock.
		/// </para>
		/// <para>
		/// If <paramref name="fileContent"/> not null, the exact file version specified will be parsed.
		/// This method will not wait for the main thread in that case.
		/// If the specified version is older than the latest version, the old version will be parsed
		/// and returned, but the old parse information will not be registered.
		/// </para>
		/// </remarks>
		ParseInformation Parse(FileName fileName, ITextSource fileContent = null, IProject parentProject = null,
		                       CancellationToken cancellationToken = default(CancellationToken));
		
		/// <summary>
		/// Parses the specified file.
		/// This method does not request full parse information.
		/// </summary>
		/// <param name="fileName">Name of the file to parse</param>
		/// <param name="fileContent">Optional: Content of the file to parse.</param>
		/// <param name="parentProject">
		/// Optional: If the file is part of multiple projects, specifies
		/// which parsed version of the file to return (for example, different project settings
		/// can cause the file to be parsed differently).
		/// </param>
		/// <returns>
		/// Returns the IParsedFile for the specified file, or null if the file cannot be parsed.
		/// For files currently open in an editor, this method does not necessarily reparse, but may return
		/// the existing IParsedFile (but only if it's still up-to-date).
		/// </returns>
		/// <remarks><inheritdoc cref="Parse"/></remarks>
		IParsedFile ParseFile(FileName fileName, ITextSource fileContent = null, IProject parentProject = null,
		                      CancellationToken cancellationToken = default(CancellationToken));
		
		/// <summary>
		/// Parses the specified file on a background thread.
		/// Produces full parse information.
		/// </summary>
		/// <param name="fileName">Name of the file to parse</param>
		/// <param name="fileContent">Optional: Content of the file to parse.</param>
		/// <param name="parentProject">
		/// Optional: If the file is part of multiple projects, specifies
		/// which parsed version of the file to return (for example, different project settings
		/// can cause the file to be parsed differently).
		/// </param>
		/// <returns><inheritdoc cref="Parse"/></returns>
		/// <remarks>
		/// This method is thread-safe.
		/// <para>
		/// If <paramref name="fileContent"/> is null, the task wait for the main thread
		/// to retrieve the latest file content.
		/// This means that waiting for the task can cause deadlocks. (however, using C# 5 <c>await</c> is safe)
		/// </para>
		/// <para>
		/// If <paramref name="fileContent"/> not null, the exact file version specified will be parsed.
		/// This method will not wait for the main thread in that case.
		/// If the specified version is older than the latest version, the old version will be parsed
		/// and returned, but the old parse information will not be registered.
		/// </para>
		/// </remarks>
		Task<ParseInformation> ParseAsync(FileName fileName, ITextSource fileContent = null, IProject parentProject = null,
		                                  CancellationToken cancellationToken = default(CancellationToken));
		
		/// <summary>
		/// Parses the specified file on a background thread.
		/// This method does not request full parse information.
		/// </summary>
		/// <param name="fileName">Name of the file to parse</param>
		/// <param name="fileContent">Optional: Content of the file to parse.</param>
		/// <param name="parentProject">
		/// Optional: If the file is part of multiple projects, specifies
		/// which parsed version of the file to return (for example, different project settings
		/// can cause the file to be parsed differently).
		/// </param>
		/// <returns><inheritdoc cref="ParseFile"/></returns>
		/// <remarks><inheritdoc cref="ParseAsync"/></remarks>
		Task<IParsedFile> ParseFileAsync(FileName fileName, ITextSource fileContent = null, IProject parentProject = null,
		                                 CancellationToken cancellationToken = default(CancellationToken));
		#endregion
		
		#region Resolve
		ResolveResult Resolve(ITextEditor editor, TextLocation location,
		                      ICompilation compilation = null,
		                      CancellationToken cancellationToken = default(CancellationToken));
		
		ResolveResult Resolve(FileName fileName, TextLocation location,
		                      ITextSource fileContent = null, ICompilation compilation = null,
		                      CancellationToken cancellationToken = default(CancellationToken));
		
		Task<ResolveResult> ResolveAsync(FileName fileName, TextLocation location,
		                                 ITextSource fileContent = null, ICompilation compilation = null,
		                                 CancellationToken cancellationToken = default(CancellationToken));
		
		Task FindLocalReferencesAsync(FileName fileName, IVariable variable, Action<Reference> callback,
		                              ITextSource fileContent = null, ICompilation compilation = null,
		                              CancellationToken cancellationToken = default(CancellationToken));
		#endregion
		
		#region Parsed File Listeners
		/// <summary>
		/// Gets whether a parser is registered for the specified file name.
		/// </summary>
		bool HasParser(FileName fileName);
		
		/// <summary>
		/// Clears the cached parse information.
		/// If the file does not belong to any project, this also clears the cached type system.
		/// </summary>
		void ClearParseInformation(FileName fileName);
		
		/// <summary>
		/// Adds a project that owns the file and wishes to receive parse information.
		/// </summary>
		/// <param name="fileName">Name of the file contained in the project.</param>
		/// <param name="project">The parent project of the file.</param>
		/// <param name="startAsyncParse">
		/// Whether to start an asynchronous parse operation for the specified file.
		/// </param>
		/// <param name="isLinkedFile">
		/// Specified whether the file is linked within the project, i.e. likely also belongs to another project.
		/// The parser services tries to use the project that contains the file directly (non-linked)
		/// as the primary parent project.
		/// </param>
		void AddOwnerProject(FileName fileName, IProject project, bool startAsyncParse, bool isLinkedFile);
		
		/// <summary>
		/// Removes a project from the owners of the file.
		/// This method invokes <c>project.UpdateParseInformation(existingParsedFile, null);</c>.
		/// (unless existingParsedFile==null)
		/// </summary>
		void RemoveOwnerProject(FileName fileName, IProject project);
		
		/// <summary>
		/// Occurs whenever parse information was updated. This event is raised on the main thread.
		/// </summary>
		event EventHandler<ParseInformationEventArgs> ParseInformationUpdated;
		#endregion
	}
}
