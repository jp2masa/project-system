﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class SharedProjectDependencyModel : DependencyModel
    {
        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: KnownMonikers.SharedProject,
            expandedIcon: KnownMonikers.SharedProject,
            unresolvedIcon: ManagedImageMonikers.SharedProjectWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.SharedProjectWarning);

        private static readonly DependencyIconSet s_implicitIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.SharedProjectPrivate,
            expandedIcon: ManagedImageMonikers.SharedProjectPrivate,
            unresolvedIcon: ManagedImageMonikers.SharedProjectWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.SharedProjectWarning);

        public SharedProjectDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            ProjectTreeFlags flags,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(providerType, path, originalItemSpec, flags, resolved, isImplicit, properties)
        {
            if (Resolved)
            {
                SchemaName = ResolvedProjectReference.SchemaName;
            }
            else
            {
                SchemaName = ProjectReference.SchemaName;
            }

            Flags = Flags.Union(DependencyTreeFlags.SharedProjectFlags)
                         .Except(DependencyTreeFlags.SupportsRuleProperties);
            Caption = System.IO.Path.GetFileNameWithoutExtension(Name);
            Priority = Dependency.ProjectNodePriority;
            SchemaItemType = ProjectReference.PrimaryDataSourceItemType;
            IconSet = isImplicit ? s_implicitIconSet : s_iconSet;
        }
    }
}
