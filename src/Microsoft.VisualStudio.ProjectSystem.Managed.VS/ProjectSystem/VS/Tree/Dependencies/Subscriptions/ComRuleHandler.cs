﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract,
            typeof(ICrossTargetRuleHandler<DependenciesRuleChangeContext>))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class ComRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "ComDependency";

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.Component,
            expandedIcon: ManagedImageMonikers.Component,
            unresolvedIcon: ManagedImageMonikers.ComponentWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.ComponentWarning);

        protected override string UnresolvedRuleName { get; } = ComReference.SchemaName;
        protected override string ResolvedRuleName { get; } = ResolvedCOMReference.SchemaName;
        public override string ProviderType { get; } = ProviderTypeString;

        public override IDependencyModel CreateRootDependencyNode()
        {
            return new SubTreeRootDependencyModel(
                ProviderType,
                VSResources.ComNodeName,
                s_iconSet,
                DependencyTreeFlags.ComSubTreeRootNodeFlags);
        }

        protected override IDependencyModel CreateDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return new ComDependencyModel(
                providerType,
                path,
                originalItemSpec,
                DependencyTreeFlags.ComSubTreeNodeFlags,
                resolved,
                isImplicit,
                properties);
        }

        public override ImageMoniker GetImplicitIcon()
        {
            return ManagedImageMonikers.ComponentPrivate;
        }
    }
}
