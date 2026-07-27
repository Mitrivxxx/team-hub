## Purpose
- Shared gRPC protobuf contracts for internal service-to-service communication.

## Source of truth
- `Protos/auth/v1/user_profile.proto`
- `Protos/organization/v1/members.proto`

## Do
- Keep contracts versioned under `auth/v1` and `organization/v1`.
- Generate both client and server stubs (`GrpcServices=Both`).
- Expose auth `UserProfileService.GetUsersByIds` for batch profile lookup.
- Expose organization `OrganizationMemberService.ListMembers` for membership lists.
- Keep contracts internal; do not expose gRPC through nginx/gateway.

## Don't
- Do not break wire compatibility without a new major proto version.
- Do not add public browser-facing APIs here.
