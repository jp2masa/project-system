﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Model for creating telemetry events when dependency tree is updated.
    /// It maintains some light state for each Target Framework to keep track
    /// whether all expected rules have been observed; this information is passed
    /// as a property of the telemetry event and can be used to determine if the
    /// 'resolved' event is fired too early (so sessions can be appropriately filtered).
    /// </summary>
    [Export(typeof(IDependencyTreeTelemetryService))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependencyTreeTelemetryService : IDependencyTreeTelemetryService
    {
        private const int MaxEventCount = 10;

        private readonly UnconfiguredProject _project;
        private readonly ITelemetryService _telemetryService;
        private readonly ConcurrentDictionary<ITargetFramework, TelemetryState> _telemetryStates =
            new ConcurrentDictionary<ITargetFramework, TelemetryState>();
        private readonly object _stateUpdateLock = new object();
        private string _projectId;
        private bool _stopTelemetry = false;
        private int _eventCount = 0;

        [ImportingConstructor]
        public DependencyTreeTelemetryService(
            UnconfiguredProject project,
            ITelemetryService telemetryService)
        {
            _project = project;
            _telemetryService = telemetryService;
        }

        /// <summary>
        /// Indicate whether we have seen all rules we initialized with, in all target frameworks
        /// </summary>
        public bool ObservedAllRules() => _telemetryStates.All(state => state.Value.ObservedAllRules());

        /// <summary>
        /// Initialize telemetry state with the set of rules we expect to observe for target framework
        /// </summary>
        public void InitializeTargetFrameworkRules(ITargetFramework targetFramework, IEnumerable<string> rules)
        {
            lock (_stateUpdateLock)
            {
                if (_stopTelemetry)
                    return;

                TelemetryState telemetryState = _telemetryStates.GetOrAdd(targetFramework, (key) => new TelemetryState());

                foreach (string rule in rules)
                {
                    telemetryState.InitializeRule(rule);
                }
            }
        }

        /// <summary>
        /// Indicate that a set of rules has been observed in either an Evaluation or Design Time pass.
        /// This information is used when firing tree update telemetry events to indicate whether all rules
        /// have been observed.
        /// </summary>
        public void ObserveTargetFrameworkRules(ITargetFramework targetFramework, IEnumerable<string> rules)
        {
            lock (_stateUpdateLock)
            {
                if (_stopTelemetry)
                    return;

                if (_telemetryStates.TryGetValue(targetFramework, out TelemetryState telemetryState))
                {
                    foreach (string rule in rules)
                    {
                        telemetryState.ObserveRule(rule);
                    }
                }
            }
        }

        /// <summary>
        /// Fire telemetry when dependency tree completes an update
        /// </summary>
        /// <param name="hasUnresolvedDependency">indicates if the snapshot used for the update had any unresolved dependencies</param>
        public void ObserveTreeUpdateCompleted(bool hasUnresolvedDependency)
        {
            bool observedAllRules;
            lock (_stateUpdateLock)
            {
                if (_stopTelemetry)
                    return;
                _stopTelemetry = !hasUnresolvedDependency || (++_eventCount >= MaxEventCount);
                observedAllRules = ObservedAllRules();
            }

            if (_projectId == null)
            {
                InitializeProjectId();
            }

            if (hasUnresolvedDependency)
            {
                _telemetryService.PostProperties(TelemetryEventName.TreeUpdatedUnresolved, new[] 
                {
                    (TelemetryPropertyName.TreeUpdatedUnresolvedProject, (object)_projectId),
                    (TelemetryPropertyName.TreeUpdatedUnresolvedObservedAllRules, observedAllRules)
                });
            }
            else
            {
                _telemetryService.PostProperties(TelemetryEventName.TreeUpdatedResolved, new[] 
                {
                    (TelemetryPropertyName.TreeUpdatedResolvedProject, (object)_projectId),
                    (TelemetryPropertyName.TreeUpdatedResolvedObservedAllRules, observedAllRules)
                });
            }
        }

        private void InitializeProjectId()
        {
            IProjectGuidService projectGuidService = _project.Services.ExportProvider.GetExportedValueOrDefault<IProjectGuidService>();
            if (projectGuidService != null)
            {
                SetProjectId(projectGuidService.ProjectGuid.ToString());
            }
            else
            {
                SetProjectId(_telemetryService.HashValue(_project.FullPath));
            }
        }

        // helper to support testing
        internal void SetProjectId(string projectId)
        {
            _projectId = projectId;
        }

        /// <summary>
        /// Maintain state for each target framework
        /// </summary>
        internal class TelemetryState
        {
            private readonly ConcurrentDictionary<string, bool> _observedRules = new ConcurrentDictionary<string, bool>(StringComparers.RuleNames);

            internal bool InitializeRule(string rule) =>
                _observedRules.TryAdd(rule, false);

            internal bool ObserveRule(string rule) =>
                _observedRules.TryUpdate(rule, true, false);

            internal bool ObservedAllRules() =>
                !_observedRules.IsEmpty && _observedRules.All(entry => entry.Value);
        }
    }
}
