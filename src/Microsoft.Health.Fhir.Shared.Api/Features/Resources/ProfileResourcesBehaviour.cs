﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Fhir.Core.Exceptions;
using Microsoft.Health.Fhir.Core.Features.Security;
using Microsoft.Health.Fhir.Core.Features.Validation;
using Microsoft.Health.Fhir.Core.Messages.Create;
using Microsoft.Health.Fhir.Core.Messages.Delete;
using Microsoft.Health.Fhir.Core.Messages.Upsert;

namespace Microsoft.Health.Fhir.Api.Features.Resources
{
    public sealed class ProfileResourcesBehaviour :
        IPipelineBehavior<CreateResourceRequest, UpsertResourceResponse>,
        IPipelineBehavior<UpsertResourceRequest, UpsertResourceResponse>,
        IPipelineBehavior<ConditionalCreateResourceRequest, UpsertResourceResponse>,
        IPipelineBehavior<ConditionalUpsertResourceRequest, UpsertResourceResponse>,
        IPipelineBehavior<DeleteResourceRequest, DeleteResourceResponse>
    {
        private IAuthorizationService<DataActions> _authorizationService;
        private IProvideProfilesForValidation _profilesResolver;

        public ProfileResourcesBehaviour(IAuthorizationService<DataActions> authorizationService, IProvideProfilesForValidation profilesResolver)
        {
            EnsureArg.IsNotNull(authorizationService, nameof(authorizationService));
            _authorizationService = authorizationService;
            _profilesResolver = profilesResolver;
        }

        public async Task<UpsertResourceResponse> Handle(ConditionalUpsertResourceRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<UpsertResourceResponse> next)
            => await GenericHandle(request.Resource.InstanceType, next, cancellationToken);

        public async Task<UpsertResourceResponse> Handle(ConditionalCreateResourceRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<UpsertResourceResponse> next)
            => await GenericHandle(request.Resource.InstanceType, next, cancellationToken);

        public async Task<UpsertResourceResponse> Handle(UpsertResourceRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<UpsertResourceResponse> next)
            => await GenericHandle(request.Resource.InstanceType, next, cancellationToken);

        public async Task<UpsertResourceResponse> Handle(CreateResourceRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<UpsertResourceResponse> next)
            => await GenericHandle(request.Resource.InstanceType, next, cancellationToken);

        public async Task<DeleteResourceResponse> Handle(DeleteResourceRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<DeleteResourceResponse> next)
            => await GenericHandle(request.ResourceKey.ResourceType, next, cancellationToken);

        private async Task<TResponse> GenericHandle<TResponse>(string resourceType, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var resources = _profilesResolver.GetProfilesTypes();
            if (resources.Contains(resourceType) && await _authorizationService.CheckAccess(DataActions.EditProfileDefinitions, cancellationToken) != DataActions.EditProfileDefinitions)
            {
                throw new UnauthorizedFhirActionException();
            }

            return await next();
        }
    }
}
