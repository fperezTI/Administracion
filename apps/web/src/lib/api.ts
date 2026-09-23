export type SystemInfoResponse = {
  product: string;
  environment: string;
  serverTimeUtc: string;
};

export type MeCompany = {
  companyId: string;
  tradeName: string;
  timeZone: string;
};

export type MeResponse = {
  userId: string;
  displayName: string;
  email: string;
  permissionCodes: string[];
  companies: MeCompany[];
  activeCompanyId: string | null;
};

export type PagedResult<T> = {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
};

export type IdentificationTechnology = "Qr" | "Barcode" | "QrAndBarcode" | "Nfc" | "Rfid";

export type PhysicalCondition = "Excellent" | "Good" | "Fair" | "Poor" | "Damaged";

export type AssetStatus =
  | "InWarehouse"
  | "Reserved"
  | "Assigned"
  | "OnLoan"
  | "InTransit"
  | "InMaintenance"
  | "UnderWarranty"
  | "Damaged"
  | "Lost"
  | "Stolen"
  | "PendingDecommission"
  | "Decommissioned"
  | "Sold"
  | "Donated"
  | "Destroyed";

export type CustomFieldDataType = "Text" | "Number" | "Date" | "Boolean" | "Select";

export type CustomFieldDefinitionSummary = {
  id: string;
  name: string;
  code: string;
  dataType: CustomFieldDataType;
  isRequired: boolean;
  sortOrder: number;
  options: string | null;
};

export type AssetCategorySummary = {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
  defaultIdentificationTechnology: IdentificationTechnology;
  customFieldCount: number;
};

export type AssetCategoryDetail = {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
  defaultIdentificationTechnology: IdentificationTechnology;
  customFields: CustomFieldDefinitionSummary[];
};

export type AssetSummary = {
  id: string;
  internalFolio: string;
  description: string | null;
  assetCategoryId: string;
  brand: string;
  model: string;
  serialNumber: string | null;
  status: AssetStatus;
  physicalCondition: PhysicalCondition;
  accessoryOfAssetId: string | null;
};

export type AssetTagInfo = {
  code: string;
  technology: IdentificationTechnology;
  printCount: number;
};

export type AssetCustomFieldValueInfo = {
  customFieldDefinitionId: string;
  value: string;
};

export type AssetAccessorySummary = {
  id: string;
  internalFolio: string;
  brand: string;
  model: string;
  status: AssetStatus;
};

export type AssetDetail = {
  id: string;
  companyId: string;
  assetCategoryId: string;
  internalFolio: string;
  patrimonialFolio: string | null;
  brand: string;
  model: string;
  serialNumber: string | null;
  description: string | null;
  status: AssetStatus;
  physicalCondition: PhysicalCondition;
  currentOrgUnitId: string | null;
  acquisitionDate: string | null;
  acquisitionCost: number | null;
  currency: string | null;
  supplier: string | null;
  invoice: string | null;
  purchaseOrder: string | null;
  warrantyStartDate: string | null;
  warrantyEndDate: string | null;
  supportContract: string | null;
  supportProvider: string | null;
  tag: AssetTagInfo | null;
  customFieldValues: AssetCustomFieldValueInfo[];
  accessoryOfAssetId: string | null;
  accessoryOfAssetFolio: string | null;
  accessories: AssetAccessorySummary[];
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

export type CreateAssetResult = {
  assetId: string;
  internalFolio: string;
  tagCode: string;
};

export type CreateAssetInput = {
  companyId: string;
  assetCategoryId: string;
  brand: string;
  model: string;
  serialNumber?: string | null;
  description?: string | null;
  physicalCondition: PhysicalCondition;
  currentOrgUnitId?: string | null;
  identificationTechnologyOverride?: IdentificationTechnology | null;
  customFieldValues?: Record<string, string> | null;
};

export class ApiError extends Error {
  constructor(
    public status: number,
    public detail: string | null,
    message: string,
  ) {
    super(message);
  }
}

/**
 * Server-side base URL for the API — reached over the Docker Compose network by service name in
 * local dev, or the App Service internal URL in Azure. Never exposed to the browser: every call here
 * runs in a Server Component, Server Action or Route Handler, never in client-side code.
 */
function getApiBaseUrl(): string {
  return process.env.API_INTERNAL_URL ?? "http://localhost:5080";
}

async function apiFetch<T>(
  accessToken: string,
  path: string,
  init?: RequestInit,
): Promise<T> {
  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    cache: "no-store",
    headers: {
      Authorization: `Bearer ${accessToken}`,
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    let detail: string | null = null;
    try {
      const problem = (await response.json()) as { detail?: string; title?: string };
      detail = problem.detail ?? problem.title ?? null;
    } catch {
      // Response had no JSON body — leave detail null.
    }
    throw new ApiError(response.status, detail, `API responded with status ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function getSystemInfo(): Promise<SystemInfoResponse> {
  const response = await fetch(`${getApiBaseUrl()}/api/v1/system/info`, { cache: "no-store" });
  if (!response.ok) {
    throw new ApiError(response.status, null, `API responded with status ${response.status}`);
  }
  return (await response.json()) as SystemInfoResponse;
}

export function getMe(accessToken: string): Promise<MeResponse> {
  return apiFetch<MeResponse>(accessToken, "/api/v1/me");
}

export type AssetSortField = "internalFolio" | "description" | "category" | "brand" | "serialNumber" | "physicalCondition" | "status";

export type GetAssetsParams = {
  companyId: string;
  pageNumber?: number;
  pageSize?: number;
  assetCategoryId?: string;
  status?: AssetStatus;
  search?: string;
  sortBy?: AssetSortField;
  sortDescending?: boolean;
};

export function getAssets(accessToken: string, params: GetAssetsParams): Promise<PagedResult<AssetSummary>> {
  const query = new URLSearchParams({ companyId: params.companyId });
  if (params.pageNumber) query.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.assetCategoryId) query.set("assetCategoryId", params.assetCategoryId);
  if (params.status) query.set("status", params.status);
  if (params.search) query.set("search", params.search);
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDescending) query.set("sortDescending", "true");

  return apiFetch<PagedResult<AssetSummary>>(accessToken, `/api/v1/assets?${query.toString()}`);
}

export function getAssetById(accessToken: string, assetId: string): Promise<AssetDetail> {
  return apiFetch<AssetDetail>(accessToken, `/api/v1/assets/${assetId}`);
}

export function createAsset(accessToken: string, input: CreateAssetInput): Promise<CreateAssetResult> {
  return apiFetch<CreateAssetResult>(accessToken, "/api/v1/assets", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function reprintAssetTag(
  accessToken: string,
  assetId: string,
): Promise<{ code: string; printCount: number }> {
  return apiFetch(accessToken, `/api/v1/assets/${assetId}/tag/reprint`, { method: "POST" });
}

export type AccessoryCandidate = { id: string; internalFolio: string; brand: string; model: string };

export function getEligibleAccessoryCandidates(
  accessToken: string,
  companyId: string,
  assetId: string,
): Promise<AccessoryCandidate[]> {
  return apiFetch<AccessoryCandidate[]>(
    accessToken, `/api/v1/assets/${assetId}/accessories/candidates?companyId=${companyId}`,
  );
}

export function linkAssetAccessory(accessToken: string, assetId: string, accessoryAssetId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/assets/${assetId}/accessories/${accessoryAssetId}`, { method: "POST" });
}

export function unlinkAssetAccessory(accessToken: string, assetId: string, accessoryAssetId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/assets/${assetId}/accessories/${accessoryAssetId}`, { method: "DELETE" });
}

export type AssetCategorySortField = "name" | "code" | "defaultIdentificationTechnology" | "customFieldCount" | "isActive";

export function getAssetCategories(
  accessToken: string,
  params?: {
    isActive?: boolean;
    pageNumber?: number;
    pageSize?: number;
    sortBy?: AssetCategorySortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<AssetCategorySummary>> {
  const query = new URLSearchParams();
  if (params?.isActive !== undefined) query.set("isActive", String(params.isActive));
  if (params?.pageNumber) query.set("pageNumber", String(params.pageNumber));
  query.set("pageSize", String(params?.pageSize ?? 100));
  if (params?.sortBy) query.set("sortBy", params.sortBy);
  if (params?.sortDescending) query.set("sortDescending", "true");

  return apiFetch<PagedResult<AssetCategorySummary>>(accessToken, `/api/v1/asset-categories?${query.toString()}`);
}

export function getAssetCategoryById(accessToken: string, categoryId: string): Promise<AssetCategoryDetail> {
  return apiFetch<AssetCategoryDetail>(accessToken, `/api/v1/asset-categories/${categoryId}`);
}

export function createAssetCategory(
  accessToken: string,
  input: { name: string; code: string; defaultIdentificationTechnology: IdentificationTechnology },
): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/asset-categories", { method: "POST", body: JSON.stringify(input) });
}

export function addCustomFieldDefinition(
  accessToken: string,
  categoryId: string,
  input: { name: string; code: string; dataType: CustomFieldDataType; isRequired: boolean; options: string | null },
): Promise<string> {
  return apiFetch<string>(accessToken, `/api/v1/asset-categories/${categoryId}/custom-fields`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function setAssetCategoryActive(accessToken: string, categoryId: string, isActive: boolean): Promise<void> {
  return apiFetch(accessToken, `/api/v1/asset-categories/${categoryId}/active`, {
    method: "PATCH",
    body: JSON.stringify(isActive),
  });
}

export type UpdateAssetGeneralInfoInput = {
  brand: string;
  model: string;
  serialNumber: string | null;
  description: string | null;
  patrimonialFolio: string | null;
  physicalCondition: PhysicalCondition;
  customFieldValues: Record<string, string> | null;
};

export function updateAssetGeneralInfo(
  accessToken: string,
  assetId: string,
  input: UpdateAssetGeneralInfoInput,
): Promise<void> {
  return apiFetch(accessToken, `/api/v1/assets/${assetId}/general`, { method: "PUT", body: JSON.stringify(input) });
}

export type UpdateAssetFinancialInfoInput = {
  acquisitionDate: string | null;
  acquisitionCost: number | null;
  currency: string | null;
  supplier: string | null;
  invoice: string | null;
  purchaseOrder: string | null;
};

export function updateAssetFinancialInfo(
  accessToken: string,
  assetId: string,
  input: UpdateAssetFinancialInfoInput,
): Promise<void> {
  return apiFetch(accessToken, `/api/v1/assets/${assetId}/financial`, { method: "PUT", body: JSON.stringify(input) });
}

export type UpdateAssetContractualInfoInput = {
  warrantyStartDate: string | null;
  warrantyEndDate: string | null;
  supportContract: string | null;
  supportProvider: string | null;
};

export function updateAssetContractualInfo(
  accessToken: string,
  assetId: string,
  input: UpdateAssetContractualInfoInput,
): Promise<void> {
  return apiFetch(accessToken, `/api/v1/assets/${assetId}/contractual`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

// --- Organization: Companies ---

export type CompanySummary = {
  id: string;
  legalName: string;
  tradeName: string;
  taxId: string;
  baseCurrency: string;
  timeZone: string;
  isActive: boolean;
};

export type CompanySortField = "tradeName" | "legalName" | "taxId" | "baseCurrency" | "timeZone" | "isActive";

export function getCompanies(
  accessToken: string,
  params?: {
    pageNumber?: number;
    pageSize?: number;
    isActive?: boolean;
    sortBy?: CompanySortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<CompanySummary>> {
  const query = new URLSearchParams();
  query.set("pageNumber", String(params?.pageNumber ?? 1));
  query.set("pageSize", String(params?.pageSize ?? 50));
  if (params?.isActive !== undefined) query.set("isActive", String(params.isActive));
  if (params?.sortBy) query.set("sortBy", params.sortBy);
  if (params?.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<CompanySummary>>(accessToken, `/api/v1/companies?${query.toString()}`);
}

export function createCompany(
  accessToken: string,
  input: { legalName: string; tradeName: string; taxId: string; baseCurrency: string; timeZone: string },
): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/companies", { method: "POST", body: JSON.stringify(input) });
}

export function getCompanyById(accessToken: string, companyId: string): Promise<CompanySummary> {
  return apiFetch<CompanySummary>(accessToken, `/api/v1/companies/${companyId}`);
}

export function setCompanyActive(accessToken: string, companyId: string, isActive: boolean): Promise<void> {
  return apiFetch(accessToken, `/api/v1/companies/${companyId}/active`, {
    method: "PATCH",
    body: JSON.stringify(isActive),
  });
}

// --- Organization: OrgUnits ---

export type OrgUnitTypeSummary = {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
};

export type OrgUnitNode = {
  id: string;
  parentOrgUnitId: string | null;
  orgUnitTypeId: string;
  orgUnitTypeName: string;
  name: string;
  code: string;
  isActive: boolean;
};

export function getOrgUnitTypes(accessToken: string): Promise<OrgUnitTypeSummary[]> {
  return apiFetch<OrgUnitTypeSummary[]>(accessToken, "/api/v1/org-units/types");
}

export function getOrgUnitTree(accessToken: string, companyId: string): Promise<OrgUnitNode[]> {
  return apiFetch<OrgUnitNode[]>(accessToken, `/api/v1/org-units?companyId=${companyId}`);
}

export function createOrgUnit(
  accessToken: string,
  input: { companyId: string; orgUnitTypeId: string; parentOrgUnitId: string | null; name: string; code: string },
): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/org-units", { method: "POST", body: JSON.stringify(input) });
}

export function moveOrgUnit(accessToken: string, orgUnitId: string, newParentOrgUnitId: string | null): Promise<void> {
  return apiFetch(accessToken, `/api/v1/org-units/${orgUnitId}/move`, {
    method: "PATCH",
    body: JSON.stringify(newParentOrgUnitId),
  });
}

export function setOrgUnitActive(accessToken: string, orgUnitId: string, isActive: boolean): Promise<void> {
  return apiFetch(accessToken, `/api/v1/org-units/${orgUnitId}/active`, {
    method: "PATCH",
    body: JSON.stringify(isActive),
  });
}

// --- Identity: Roles & Permissions ---

export type RoleSummary = {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
  permissionCount: number;
};

export type RoleDetail = {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
  permissionIds: string[];
};

export type PermissionSummary = {
  id: string;
  module: string;
  action: string;
  code: string;
  description: string;
};

export type PermissionModuleGroup = {
  module: string;
  permissions: PermissionSummary[];
};

export type RoleSortField = "name" | "description" | "permissionCount" | "isActive";

export function getRoles(
  accessToken: string,
  params?: {
    pageNumber?: number;
    pageSize?: number;
    isActive?: boolean;
    sortBy?: RoleSortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<RoleSummary>> {
  const query = new URLSearchParams();
  query.set("pageNumber", String(params?.pageNumber ?? 1));
  query.set("pageSize", String(params?.pageSize ?? 50));
  if (params?.isActive !== undefined) query.set("isActive", String(params.isActive));
  if (params?.sortBy) query.set("sortBy", params.sortBy);
  if (params?.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<RoleSummary>>(accessToken, `/api/v1/roles?${query.toString()}`);
}

export function getRoleById(accessToken: string, roleId: string): Promise<RoleDetail> {
  return apiFetch<RoleDetail>(accessToken, `/api/v1/roles/${roleId}`);
}

export function createRole(accessToken: string, input: { name: string; description: string | null }): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/roles", { method: "POST", body: JSON.stringify(input) });
}

export function duplicateRole(accessToken: string, sourceRoleId: string, newName: string): Promise<string> {
  return apiFetch<string>(accessToken, `/api/v1/roles/${sourceRoleId}/duplicate`, {
    method: "POST",
    body: JSON.stringify(newName),
  });
}

export function updateRole(
  accessToken: string,
  roleId: string,
  input: { name: string; description: string | null },
): Promise<void> {
  return apiFetch(accessToken, `/api/v1/roles/${roleId}`, { method: "PUT", body: JSON.stringify(input) });
}

export function setRoleActive(accessToken: string, roleId: string, isActive: boolean): Promise<void> {
  return apiFetch(accessToken, `/api/v1/roles/${roleId}/active`, { method: "PATCH", body: JSON.stringify(isActive) });
}

export function setRolePermissions(accessToken: string, roleId: string, permissionIds: string[]): Promise<void> {
  return apiFetch(accessToken, `/api/v1/roles/${roleId}/permissions`, {
    method: "PUT",
    body: JSON.stringify(permissionIds),
  });
}

export function getPermissions(accessToken: string): Promise<PermissionModuleGroup[]> {
  return apiFetch<PermissionModuleGroup[]>(accessToken, "/api/v1/permissions");
}

// --- System configuration ---

export type SystemSettings = {
  senderMailbox: string;
  graphTenantId: string;
  graphClientId: string;
  hasGraphClientSecretConfigured: boolean;
};

export type UpdateSystemSettingsInput = {
  senderMailbox: string | null;
  graphTenantId: string | null;
  graphClientId: string | null;
  /** null/empty leaves the previously stored secret unchanged — never pre-filled from a GET. */
  graphClientSecret: string | null;
};

export function getSystemSettings(accessToken: string): Promise<SystemSettings> {
  return apiFetch<SystemSettings>(accessToken, "/api/v1/system/settings");
}

export function updateSystemSettings(accessToken: string, input: UpdateSystemSettingsInput): Promise<void> {
  return apiFetch(accessToken, "/api/v1/system/settings", { method: "PUT", body: JSON.stringify(input) });
}

// --- Identity: Users ---

export type UserSummary = {
  id: string;
  displayName: string;
  email: string;
  isActive: boolean;
  lastLoginAtUtc: string | null;
};

export type UserDetail = {
  id: string;
  displayName: string;
  email: string;
  isActive: boolean;
  lastLoginAtUtc: string | null;
  anonymizedAtUtc: string | null;
  roleIds: string[];
  companyIds: string[];
};

export type UserSortField = "displayName" | "email" | "isActive" | "lastLoginAtUtc";

export function getUsers(
  accessToken: string,
  params?: {
    pageNumber?: number;
    pageSize?: number;
    isActive?: boolean;
    sortBy?: UserSortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<UserSummary>> {
  const query = new URLSearchParams();
  query.set("pageNumber", String(params?.pageNumber ?? 1));
  query.set("pageSize", String(params?.pageSize ?? 50));
  if (params?.isActive !== undefined) query.set("isActive", String(params.isActive));
  if (params?.sortBy) query.set("sortBy", params.sortBy);
  if (params?.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<UserSummary>>(accessToken, `/api/v1/users?${query.toString()}`);
}

export function getUserById(accessToken: string, userId: string): Promise<UserDetail> {
  return apiFetch<UserDetail>(accessToken, `/api/v1/users/${userId}`);
}

export type DirectoryUser = { entraObjectId: string; displayName: string; email: string };

export function searchDirectoryUsers(accessToken: string, query: string): Promise<DirectoryUser[]> {
  return apiFetch<DirectoryUser[]>(accessToken, `/api/v1/users/directory/search?query=${encodeURIComponent(query)}`);
}

export function createUserFromDirectory(
  accessToken: string,
  input: { entraObjectId: string; displayName: string; email: string; roleId: string; companyIds: string[] },
): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/users/directory", { method: "POST", body: JSON.stringify(input) });
}

export function assignRoleToUser(accessToken: string, userId: string, roleId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/users/${userId}/roles/${roleId}`, { method: "POST" });
}

export function removeRoleFromUser(accessToken: string, userId: string, roleId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/users/${userId}/roles/${roleId}`, { method: "DELETE" });
}

export function grantUserCompanyAccess(accessToken: string, userId: string, companyId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/users/${userId}/companies/${companyId}`, { method: "POST" });
}

export function revokeUserCompanyAccess(accessToken: string, userId: string, companyId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/users/${userId}/companies/${companyId}`, { method: "DELETE" });
}

/** Permanent PII removal (F12, ver docs/privacy-retention.md) — irreversible. */
export function anonymizeUser(accessToken: string, userId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/users/${userId}/anonymize`, { method: "POST" });
}

// --- Inventory Operations: Assignments, Loans, Movements ---

export type AssignmentStatus = "PendingSignature" | "Accepted" | "Returned" | "Cancelled";

export type LoanStatus = "Active" | "Returned";

export type MovementType = "Assignment" | "AssignmentReturn" | "Loan" | "LoanReturn" | "Relocation";

export type MovementStatus = "Pending" | "Completed" | "Cancelled";

export type AssignmentSummary = {
  id: string;
  assetId: string;
  assetFolio: string;
  assignedToUserId: string;
  assignedToDisplayName: string;
  status: AssignmentStatus;
  assignedAtUtc: string;
  acceptedAtUtc: string | null;
  returnedAtUtc: string | null;
  groupId: string | null;
};

export type SignatureInfo = {
  signerDisplayName: string;
  signedAtUtc: string;
  contentHash: string;
};

export type AssignmentGroupMember = {
  assetId: string;
  assetFolio: string;
  patrimonialFolio: string | null;
  brand: string;
  model: string;
  serialNumber: string | null;
  isPrimary: boolean;
};

export type AssignmentDetail = {
  id: string;
  companyId: string;
  assetId: string;
  assetFolio: string;
  assetPatrimonialFolio: string | null;
  assetBrand: string;
  assetModel: string;
  assetSerialNumber: string | null;
  assetDescription: string | null;
  assignedToUserId: string;
  assignedToDisplayName: string;
  assignedToEmail: string;
  orgUnitId: string | null;
  orgUnitName: string | null;
  movementFolio: string;
  status: AssignmentStatus;
  assignedAtUtc: string;
  acceptedAtUtc: string | null;
  returnedAtUtc: string | null;
  acceptanceSignature: SignatureInfo | null;
  returnSignature: SignatureInfo | null;
  groupMembers: AssignmentGroupMember[];
};

export type MyAssignmentSummary = {
  id: string;
  assetId: string;
  assetFolio: string;
  assetBrand: string;
  assetModel: string;
  status: AssignmentStatus;
  assignedAtUtc: string;
  groupId: string | null;
};

export type LoanSummary = {
  id: string;
  assetId: string;
  assetFolio: string;
  borrowerUserId: string;
  borrowerDisplayName: string;
  expectedReturnDate: string;
  status: LoanStatus;
  loanedAtUtc: string;
  returnedAtUtc: string | null;
};

export type LoanDetail = LoanSummary & { companyId: string };

export type MovementSummary = {
  id: string;
  assetId: string;
  assetFolio: string;
  type: MovementType;
  folioNumber: string;
  status: MovementStatus;
  fromOrgUnitId: string | null;
  toOrgUnitId: string | null;
  fromUserId: string | null;
  toUserId: string | null;
  notes: string | null;
  effectiveAtUtc: string;
};

export type CreateAssignmentResult = { assignmentId: string; movementId: string; movementFolio: string };

export type CreateLoanResult = { loanId: string; movementId: string; movementFolio: string };

export type RelocateAssetResult = { movementId: string; movementFolio: string };

export type AssignmentSortField = "assignedAtUtc" | "assetFolio" | "assignedToDisplayName" | "status";

export function getAssignments(
  accessToken: string,
  params: {
    companyId: string;
    pageNumber?: number;
    pageSize?: number;
    status?: AssignmentStatus;
    assetId?: string;
    sortBy?: AssignmentSortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<AssignmentSummary>> {
  const query = new URLSearchParams({ companyId: params.companyId });
  if (params.pageNumber) query.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.status) query.set("status", params.status);
  if (params.assetId) query.set("assetId", params.assetId);
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<AssignmentSummary>>(accessToken, `/api/v1/assignments?${query.toString()}`);
}

export function getMyAssignments(accessToken: string): Promise<MyAssignmentSummary[]> {
  return apiFetch<MyAssignmentSummary[]>(accessToken, "/api/v1/assignments/mine");
}

export function getAssignmentById(accessToken: string, assignmentId: string): Promise<AssignmentDetail> {
  return apiFetch<AssignmentDetail>(accessToken, `/api/v1/assignments/${assignmentId}`);
}

/** Self-service: only the caller's own assignment — the report behind the "confirm what was assigned to
 * me" email link. Same shape as {@link getAssignmentById}, just gated by ownership instead of RBAC. */
export function getMyAssignmentById(accessToken: string, assignmentId: string): Promise<AssignmentDetail> {
  return apiFetch<AssignmentDetail>(accessToken, `/api/v1/assignments/mine/${assignmentId}`);
}

export function createAssignment(
  accessToken: string,
  input: {
    assetId: string;
    assignedToUserId: string;
    orgUnitId: string | null;
    notes: string | null;
    accessoryAssetIds?: string[];
  },
): Promise<CreateAssignmentResult> {
  return apiFetch<CreateAssignmentResult>(accessToken, "/api/v1/assignments", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function signAssignment(accessToken: string, assignmentId: string, typedFullName: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/assignments/${assignmentId}/sign`, {
    method: "POST",
    body: JSON.stringify({ typedFullName }),
  });
}

export function cancelAssignment(accessToken: string, assignmentId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/assignments/${assignmentId}/cancel`, { method: "POST" });
}

/** Self-service — only the assignment's own recipient can call this successfully. */
export function rejectAssignment(accessToken: string, assignmentId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/assignments/${assignmentId}/reject`, { method: "POST" });
}

export function returnAssignment(
  accessToken: string,
  assignmentId: string,
  typedFullName: string,
  notes: string | null,
): Promise<void> {
  return apiFetch(accessToken, `/api/v1/assignments/${assignmentId}/return`, {
    method: "POST",
    body: JSON.stringify({ typedFullName, notes }),
  });
}

export function reassignAsset(
  accessToken: string,
  input: {
    assetId: string;
    newAssignedToUserId: string;
    orgUnitId: string | null;
    typedFullName: string;
    notes: string | null;
    accessoryAssetIds?: string[];
  },
): Promise<CreateAssignmentResult> {
  return apiFetch<CreateAssignmentResult>(accessToken, "/api/v1/assignments/reassign", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export type LoanSortField = "loanedAtUtc" | "assetFolio" | "borrowerDisplayName" | "expectedReturnDate" | "status";

export function getLoans(
  accessToken: string,
  params: {
    companyId: string;
    pageNumber?: number;
    pageSize?: number;
    status?: LoanStatus;
    sortBy?: LoanSortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<LoanSummary>> {
  const query = new URLSearchParams({ companyId: params.companyId });
  if (params.pageNumber) query.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.status) query.set("status", params.status);
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<LoanSummary>>(accessToken, `/api/v1/loans?${query.toString()}`);
}

export function getLoanById(accessToken: string, loanId: string): Promise<LoanDetail> {
  return apiFetch<LoanDetail>(accessToken, `/api/v1/loans/${loanId}`);
}

export function createLoan(
  accessToken: string,
  input: { assetId: string; borrowerUserId: string; expectedReturnDate: string; notes: string | null },
): Promise<CreateLoanResult> {
  return apiFetch<CreateLoanResult>(accessToken, "/api/v1/loans", { method: "POST", body: JSON.stringify(input) });
}

export function returnLoan(accessToken: string, loanId: string, notes: string | null): Promise<void> {
  return apiFetch(accessToken, `/api/v1/loans/${loanId}/return`, { method: "POST", body: JSON.stringify({ notes }) });
}

export type MovementSortField = "effectiveAtUtc" | "folioNumber" | "assetFolio" | "type" | "notes";

export function getMovements(
  accessToken: string,
  params: {
    companyId: string;
    pageNumber?: number;
    pageSize?: number;
    assetId?: string;
    type?: MovementType;
    sortBy?: MovementSortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<MovementSummary>> {
  const query = new URLSearchParams({ companyId: params.companyId });
  if (params.pageNumber) query.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.assetId) query.set("assetId", params.assetId);
  if (params.type) query.set("type", params.type);
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<MovementSummary>>(accessToken, `/api/v1/movements?${query.toString()}`);
}

export function relocateAsset(
  accessToken: string,
  assetId: string,
  newOrgUnitId: string | null,
  notes: string | null,
): Promise<RelocateAssetResult> {
  return apiFetch<RelocateAssetResult>(accessToken, `/api/v1/assets/${assetId}/relocate`, {
    method: "POST",
    body: JSON.stringify({ newOrgUnitId, notes }),
  });
}

export function requestAssetDecommission(accessToken: string, assetId: string, justification: string): Promise<string> {
  return apiFetch<string>(accessToken, `/api/v1/assets/${assetId}/decommission`, {
    method: "POST",
    body: JSON.stringify({ justification }),
  });
}

export function requestAssetDisposal(
  accessToken: string,
  assetId: string,
  targetStatus: "Sold" | "Donated" | "Destroyed",
  justification: string,
): Promise<string> {
  return apiFetch<string>(accessToken, `/api/v1/assets/${assetId}/dispose`, {
    method: "POST",
    body: JSON.stringify({ targetStatus, justification }),
  });
}

// --- Approvals ---

export type ApprovalMode = "Sequential" | "Parallel";

export type ApprovalInstanceStatus = "Pending" | "Approved" | "Rejected" | "Cancelled";

export type ApprovalStepDecision = "Approved" | "Rejected";

export type SignatureMechanism = "TypedConfirmation" | "DrawnSignature";

export type ApproverRoleInfo = { roleId: string; roleName: string };

export type ApprovalFlowDefinitionSummary = {
  id: string;
  key: string;
  companyId: string | null;
  approverRoles: ApproverRoleInfo[];
  requiredApprovals: number;
  mode: ApprovalMode;
  requiresComment: boolean;
  isActive: boolean;
};

export type ApprovalStepInfo = {
  approverUserId: string;
  approverDisplayName: string;
  decision: string;
  comment: string | null;
  decidedAtUtc: string;
};

export type ApprovalInstanceDetail = {
  id: string;
  contextType: string;
  contextId: string;
  requestedByUserId: string;
  requestedByDisplayName: string;
  comment: string | null;
  status: ApprovalInstanceStatus;
  mode: ApprovalMode;
  requiredApprovals: number;
  steps: ApprovalStepInfo[];
  createdAtUtc: string;
};

export type MyPendingApprovalSummary = {
  id: string;
  contextType: string;
  contextId: string;
  requestedByDisplayName: string;
  comment: string | null;
  createdAtUtc: string;
};

export type ApprovalFlowSortField = "key" | "companyId" | "mode" | "requiredApprovals" | "isActive";

export function getApprovalFlows(
  accessToken: string,
  params?: { pageNumber?: number; pageSize?: number; sortBy?: ApprovalFlowSortField; sortDescending?: boolean },
): Promise<PagedResult<ApprovalFlowDefinitionSummary>> {
  const query = new URLSearchParams();
  query.set("pageNumber", String(params?.pageNumber ?? 1));
  query.set("pageSize", String(params?.pageSize ?? 50));
  if (params?.sortBy) query.set("sortBy", params.sortBy);
  if (params?.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<ApprovalFlowDefinitionSummary>>(accessToken, `/api/v1/approval-flows?${query.toString()}`);
}

export function createApprovalFlow(
  accessToken: string,
  input: {
    key: string;
    companyId: string | null;
    approverRoleIds: string[];
    requiredApprovals: number;
    mode: ApprovalMode;
    requiresComment: boolean;
  },
): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/approval-flows", { method: "POST", body: JSON.stringify(input) });
}

export function setApprovalFlowActive(accessToken: string, flowDefinitionId: string, isActive: boolean): Promise<void> {
  return apiFetch(accessToken, `/api/v1/approval-flows/${flowDefinitionId}/active`, {
    method: "PATCH",
    body: JSON.stringify(isActive),
  });
}

export function getMyPendingApprovals(accessToken: string): Promise<MyPendingApprovalSummary[]> {
  return apiFetch<MyPendingApprovalSummary[]>(accessToken, "/api/v1/approvals/mine");
}

export function getApprovalInstanceById(accessToken: string, approvalInstanceId: string): Promise<ApprovalInstanceDetail> {
  return apiFetch<ApprovalInstanceDetail>(accessToken, `/api/v1/approvals/${approvalInstanceId}`);
}

export type DecideApprovalInput = {
  comment: string | null;
  signatureMechanism: SignatureMechanism;
  typedFullName: string | null;
  signatureImageDataUrl: string | null;
};

export function approveApprovalStep(accessToken: string, approvalInstanceId: string, input: DecideApprovalInput): Promise<void> {
  return apiFetch(accessToken, `/api/v1/approvals/${approvalInstanceId}/approve`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function rejectApprovalStep(accessToken: string, approvalInstanceId: string, input: DecideApprovalInput): Promise<void> {
  return apiFetch(accessToken, `/api/v1/approvals/${approvalInstanceId}/reject`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function cancelApprovalInstance(accessToken: string, approvalInstanceId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/approvals/${approvalInstanceId}/cancel`, { method: "POST" });
}

// --- Templates ---

export type TemplateSummary = { id: string; key: string; name: string; isActive: boolean; latestVersionNumber: number };

export type TemplateVersionInfo = { versionNumber: number; content: string; createdAtUtc: string };

export type TemplateDetail = { id: string; key: string; name: string; isActive: boolean; versions: TemplateVersionInfo[] };

export type TemplateSortField = "name" | "key" | "latestVersionNumber" | "isActive";

export function getTemplates(
  accessToken: string,
  params?: { pageNumber?: number; pageSize?: number; sortBy?: TemplateSortField; sortDescending?: boolean },
): Promise<PagedResult<TemplateSummary>> {
  const query = new URLSearchParams();
  query.set("pageNumber", String(params?.pageNumber ?? 1));
  query.set("pageSize", String(params?.pageSize ?? 50));
  if (params?.sortBy) query.set("sortBy", params.sortBy);
  if (params?.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<TemplateSummary>>(accessToken, `/api/v1/templates?${query.toString()}`);
}

export function getTemplateById(accessToken: string, templateId: string): Promise<TemplateDetail> {
  return apiFetch<TemplateDetail>(accessToken, `/api/v1/templates/${templateId}`);
}

export function createTemplate(
  accessToken: string,
  input: { key: string; name: string; initialContent: string },
): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/templates", { method: "POST", body: JSON.stringify(input) });
}

export function addTemplateVersion(accessToken: string, templateId: string, content: string): Promise<number> {
  return apiFetch<number>(accessToken, `/api/v1/templates/${templateId}/versions`, {
    method: "POST",
    body: JSON.stringify({ content }),
  });
}

export function setTemplateActive(accessToken: string, templateId: string, isActive: boolean): Promise<void> {
  return apiFetch(accessToken, `/api/v1/templates/${templateId}/active`, { method: "PATCH", body: JSON.stringify(isActive) });
}

// --- Cross-company Transfers (F5) ---

export type TransferStatus = "PendingApproval" | "Rejected" | "Cancelled" | "InTransit" | "Completed";

export type TransferSummary = {
  id: string;
  assetId: string;
  assetFolio: string;
  fromCompanyId: string;
  fromCompanyName: string;
  toCompanyId: string;
  toCompanyName: string;
  status: TransferStatus;
  requestedAtUtc: string;
};

export type TransferDetail = {
  id: string;
  assetId: string;
  assetFolio: string;
  fromCompanyId: string;
  fromCompanyName: string;
  toCompanyId: string;
  toCompanyName: string;
  requestedByUserId: string;
  requestedByDisplayName: string;
  notes: string | null;
  status: TransferStatus;
  requestedAtUtc: string;
  departedAtUtc: string | null;
  completedAtUtc: string | null;
};

export type TransferSortField = "requestedAtUtc" | "assetFolio" | "fromCompanyName" | "toCompanyName" | "status";

export function getTransfers(
  accessToken: string,
  params: {
    companyId: string;
    pageNumber?: number;
    pageSize?: number;
    status?: TransferStatus;
    sortBy?: TransferSortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<TransferSummary>> {
  const query = new URLSearchParams({ companyId: params.companyId });
  if (params.pageNumber) query.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.status) query.set("status", params.status);
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<TransferSummary>>(accessToken, `/api/v1/transfers?${query.toString()}`);
}

export function getTransferById(accessToken: string, transferId: string): Promise<TransferDetail> {
  return apiFetch<TransferDetail>(accessToken, `/api/v1/transfers/${transferId}`);
}

export function requestTransfer(
  accessToken: string,
  input: { assetId: string; toCompanyId: string; notes: string | null },
): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/transfers", { method: "POST", body: JSON.stringify(input) });
}

export function receiveTransfer(accessToken: string, transferId: string, input: DecideApprovalInput): Promise<void> {
  return apiFetch(accessToken, `/api/v1/transfers/${transferId}/receive`, {
    method: "POST",
    body: JSON.stringify({
      signatureMechanism: input.signatureMechanism,
      typedFullName: input.typedFullName,
      signatureImageDataUrl: input.signatureImageDataUrl,
    }),
  });
}

export function cancelTransfer(accessToken: string, transferId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/transfers/${transferId}/cancel`, { method: "POST" });
}

// --- Maintenance (F6) ---

export type MaintenanceOrderType = "Preventive" | "Corrective";

export type MaintenanceOrderStatus = "Open" | "Closed";

export type MaintenanceOrderSummary = {
  id: string;
  assetId: string;
  assetFolio: string;
  folio: string;
  type: MaintenanceOrderType;
  status: MaintenanceOrderStatus;
  openedAtUtc: string;
  closedAtUtc: string | null;
};

export type MaintenanceOrderChecklistResultInfo = {
  itemIndex: number;
  itemText: string;
  isCompleted: boolean;
  notes: string | null;
};

export type MaintenanceOrderDetail = {
  id: string;
  companyId: string;
  assetId: string;
  assetFolio: string;
  folio: string;
  type: MaintenanceOrderType;
  status: MaintenanceOrderStatus;
  description: string;
  checklistDefinitionId: string | null;
  checklistVersionNumber: number | null;
  checklistResults: MaintenanceOrderChecklistResultInfo[];
  openedAtUtc: string;
  closedAtUtc: string | null;
  resultStatus: AssetStatus | null;
  resultNotes: string | null;
};

export type MaintenanceOrderSortField = "openedAtUtc" | "folio" | "assetFolio" | "type" | "status";

export function getMaintenanceOrders(
  accessToken: string,
  params: {
    companyId: string;
    pageNumber?: number;
    pageSize?: number;
    assetId?: string;
    status?: MaintenanceOrderStatus;
    sortBy?: MaintenanceOrderSortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<MaintenanceOrderSummary>> {
  const query = new URLSearchParams({ companyId: params.companyId });
  if (params.pageNumber) query.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.assetId) query.set("assetId", params.assetId);
  if (params.status) query.set("status", params.status);
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<MaintenanceOrderSummary>>(accessToken, `/api/v1/maintenance-orders?${query.toString()}`);
}

export function getMaintenanceOrderById(accessToken: string, maintenanceOrderId: string): Promise<MaintenanceOrderDetail> {
  return apiFetch<MaintenanceOrderDetail>(accessToken, `/api/v1/maintenance-orders/${maintenanceOrderId}`);
}

export function openMaintenanceOrder(
  accessToken: string,
  input: { assetId: string; type: MaintenanceOrderType; description: string; checklistDefinitionId: string | null },
): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/maintenance-orders", { method: "POST", body: JSON.stringify(input) });
}

export function closeMaintenanceOrder(
  accessToken: string,
  maintenanceOrderId: string,
  input: {
    resultStatus: AssetStatus;
    resultNotes: string;
    checklistItemResults: { itemIndex: number; isCompleted: boolean; notes: string | null }[] | null;
  },
): Promise<void> {
  return apiFetch(accessToken, `/api/v1/maintenance-orders/${maintenanceOrderId}/close`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export type MaintenanceChecklistDefinitionSummary = {
  id: string;
  key: string;
  name: string;
  assetCategoryId: string | null;
  isActive: boolean;
  latestVersionNumber: number;
};

export type MaintenanceChecklistVersionInfo = {
  versionNumber: number;
  items: string[];
  createdAtUtc: string;
};

export type MaintenanceChecklistDefinitionDetail = {
  id: string;
  key: string;
  name: string;
  assetCategoryId: string | null;
  isActive: boolean;
  versions: MaintenanceChecklistVersionInfo[];
};

export type MaintenanceChecklistSortField = "name" | "key" | "latestVersionNumber" | "isActive";

export function getMaintenanceChecklists(
  accessToken: string,
  params?: { pageNumber?: number; pageSize?: number; sortBy?: MaintenanceChecklistSortField; sortDescending?: boolean },
): Promise<PagedResult<MaintenanceChecklistDefinitionSummary>> {
  const query = new URLSearchParams();
  query.set("pageNumber", String(params?.pageNumber ?? 1));
  query.set("pageSize", String(params?.pageSize ?? 50));
  if (params?.sortBy) query.set("sortBy", params.sortBy);
  if (params?.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<MaintenanceChecklistDefinitionSummary>>(
    accessToken,
    `/api/v1/maintenance-checklists?${query.toString()}`,
  );
}

export function getMaintenanceChecklistById(
  accessToken: string,
  checklistDefinitionId: string,
): Promise<MaintenanceChecklistDefinitionDetail> {
  return apiFetch<MaintenanceChecklistDefinitionDetail>(accessToken, `/api/v1/maintenance-checklists/${checklistDefinitionId}`);
}

export function createMaintenanceChecklist(
  accessToken: string,
  input: { key: string; name: string; assetCategoryId: string | null; initialItems: string[] },
): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/maintenance-checklists", { method: "POST", body: JSON.stringify(input) });
}

export function addMaintenanceChecklistVersion(
  accessToken: string,
  checklistDefinitionId: string,
  items: string[],
): Promise<number> {
  return apiFetch<number>(accessToken, `/api/v1/maintenance-checklists/${checklistDefinitionId}/versions`, {
    method: "POST",
    body: JSON.stringify({ items }),
  });
}

export function setMaintenanceChecklistActive(
  accessToken: string,
  checklistDefinitionId: string,
  isActive: boolean,
): Promise<void> {
  return apiFetch(accessToken, `/api/v1/maintenance-checklists/${checklistDefinitionId}/active`, {
    method: "PATCH",
    body: JSON.stringify(isActive),
  });
}

export type WarrantyType = "Manufacturer" | "Extended" | "ThirdParty";

export type WarrantySummary = {
  id: string;
  assetId: string;
  assetFolio: string;
  type: WarrantyType;
  provider: string;
  startDate: string;
  endDate: string;
  terms: string | null;
};

export type WarrantySortField = "endDate" | "assetFolio" | "type" | "provider";

export function getWarranties(
  accessToken: string,
  params: {
    companyId: string;
    assetId?: string;
    pageNumber?: number;
    pageSize?: number;
    sortBy?: WarrantySortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<WarrantySummary>> {
  const query = new URLSearchParams({ companyId: params.companyId });
  if (params.assetId) query.set("assetId", params.assetId);
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 50));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<WarrantySummary>>(accessToken, `/api/v1/warranties?${query.toString()}`);
}

export function createWarranty(
  accessToken: string,
  input: { assetId: string; type: WarrantyType; provider: string; startDate: string; endDate: string; terms: string | null },
): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/warranties", { method: "POST", body: JSON.stringify(input) });
}

export function updateWarranty(
  accessToken: string,
  warrantyId: string,
  input: { type: WarrantyType; provider: string; startDate: string; endDate: string; terms: string | null },
): Promise<void> {
  return apiFetch(accessToken, `/api/v1/warranties/${warrantyId}`, { method: "PUT", body: JSON.stringify(input) });
}

// --- Spare Parts & Consumables (F6) ---

export type SparePartStatus = "InStock" | "Installed" | "Disposed";

export type SparePartSummary = {
  id: string;
  name: string;
  partNumber: string | null;
  serialNumber: string;
  status: SparePartStatus;
  currentAssetId: string | null;
  currentWarehouseOrgUnitId: string | null;
};

export type SparePartInstallationInfo = {
  assetId: string;
  assetFolio: string;
  maintenanceOrderId: string | null;
  installedAtUtc: string;
  removedAtUtc: string | null;
};

export type SparePartDetail = {
  id: string;
  companyId: string;
  name: string;
  partNumber: string | null;
  serialNumber: string;
  status: SparePartStatus;
  currentAssetId: string | null;
  currentWarehouseOrgUnitId: string | null;
  installations: SparePartInstallationInfo[];
};

export type SparePartSortField = "name" | "serialNumber" | "status";

export function getSpareParts(
  accessToken: string,
  params: {
    companyId: string;
    status?: SparePartStatus;
    pageNumber?: number;
    pageSize?: number;
    sortBy?: SparePartSortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<SparePartSummary>> {
  const query = new URLSearchParams({ companyId: params.companyId });
  if (params.status) query.set("status", params.status);
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 50));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<SparePartSummary>>(accessToken, `/api/v1/spare-parts?${query.toString()}`);
}

export function getSparePartById(accessToken: string, sparePartId: string): Promise<SparePartDetail> {
  return apiFetch<SparePartDetail>(accessToken, `/api/v1/spare-parts/${sparePartId}`);
}

export function createSparePart(
  accessToken: string,
  input: { companyId: string; name: string; partNumber: string | null; serialNumber: string; warehouseOrgUnitId: string },
): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/spare-parts", { method: "POST", body: JSON.stringify(input) });
}

export function installSparePart(
  accessToken: string,
  sparePartId: string,
  input: { assetId: string; maintenanceOrderId: string | null },
): Promise<void> {
  return apiFetch(accessToken, `/api/v1/spare-parts/${sparePartId}/install`, { method: "POST", body: JSON.stringify(input) });
}

export function uninstallSparePart(accessToken: string, sparePartId: string, warehouseOrgUnitId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/spare-parts/${sparePartId}/uninstall`, {
    method: "POST",
    body: JSON.stringify({ warehouseOrgUnitId }),
  });
}

export function disposeSparePart(accessToken: string, sparePartId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/spare-parts/${sparePartId}/dispose`, { method: "POST" });
}

export type ConsumableSummary = {
  id: string;
  name: string;
  sku: string | null;
  unitOfMeasure: string;
  minimumStock: number | null;
  currentStock: number;
};

export type ConsumableStockDirection = "In" | "Out";

export type ConsumableStockMovementReason = "InitialStock" | "Purchase" | "Consumption" | "Adjustment";

export type ConsumableStockMovementSummary = {
  id: string;
  folio: string;
  warehouseOrgUnitId: string;
  direction: ConsumableStockDirection;
  reason: ConsumableStockMovementReason;
  quantity: number;
  referenceMaintenanceOrderId: string | null;
  notes: string | null;
  occurredAtUtc: string;
};

export type ConsumableSortField = "name" | "sku" | "currentStock" | "minimumStock";

export function getConsumables(
  accessToken: string,
  companyId: string,
  params?: { pageNumber?: number; pageSize?: number; sortBy?: ConsumableSortField; sortDescending?: boolean },
): Promise<PagedResult<ConsumableSummary>> {
  const query = new URLSearchParams({ companyId });
  query.set("pageNumber", String(params?.pageNumber ?? 1));
  query.set("pageSize", String(params?.pageSize ?? 50));
  if (params?.sortBy) query.set("sortBy", params.sortBy);
  if (params?.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<ConsumableSummary>>(accessToken, `/api/v1/consumables?${query.toString()}`);
}

export function getConsumableStockMovements(accessToken: string, consumableId: string): Promise<ConsumableStockMovementSummary[]> {
  return apiFetch<ConsumableStockMovementSummary[]>(accessToken, `/api/v1/consumables/${consumableId}/movements`);
}

export function createConsumable(
  accessToken: string,
  input: { companyId: string; name: string; sku: string | null; unitOfMeasure: string; minimumStock: number | null },
): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/consumables", { method: "POST", body: JSON.stringify(input) });
}

export function updateConsumable(
  accessToken: string,
  consumableId: string,
  input: { name: string; sku: string | null; unitOfMeasure: string; minimumStock: number | null },
): Promise<void> {
  return apiFetch(accessToken, `/api/v1/consumables/${consumableId}`, { method: "PUT", body: JSON.stringify(input) });
}

export function registerConsumableStockMovement(
  accessToken: string,
  consumableId: string,
  input: {
    warehouseOrgUnitId: string;
    direction: ConsumableStockDirection;
    reason: ConsumableStockMovementReason;
    quantity: number;
    referenceMaintenanceOrderId: string | null;
    notes: string | null;
  },
): Promise<string> {
  return apiFetch<string>(accessToken, `/api/v1/consumables/${consumableId}/movements`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

// --- Internal Requests (F7) ---

export type InternalRequestType = "AssetAssignment" | "Loan" | "Maintenance";

export type InternalRequestStatus = "PendingApproval" | "Rejected" | "Cancelled" | "Fulfilled";

export type InternalRequestSummary = {
  id: string;
  type: InternalRequestType;
  assetId: string;
  assetFolio: string;
  requestedByUserId: string;
  requestedByDisplayName: string;
  status: InternalRequestStatus;
  requestedAtUtc: string;
};

export type MyInternalRequestSummary = {
  id: string;
  type: InternalRequestType;
  assetId: string;
  assetFolio: string;
  justification: string;
  expectedReturnDate: string | null;
  status: InternalRequestStatus;
  requestedAtUtc: string;
};

export type InternalRequestDetail = {
  id: string;
  companyId: string;
  type: InternalRequestType;
  assetId: string;
  assetFolio: string;
  requestedByUserId: string;
  requestedByDisplayName: string;
  justification: string;
  expectedReturnDate: string | null;
  status: InternalRequestStatus;
  fulfillmentReferenceId: string | null;
  requestedAtUtc: string;
  decidedAtUtc: string | null;
};

export type InternalRequestSortField = "requestedAtUtc" | "assetFolio" | "type" | "requestedByDisplayName" | "status";

export function getInternalRequests(
  accessToken: string,
  params: {
    companyId: string;
    pageNumber?: number;
    pageSize?: number;
    status?: InternalRequestStatus;
    sortBy?: InternalRequestSortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<InternalRequestSummary>> {
  const query = new URLSearchParams({ companyId: params.companyId });
  if (params.pageNumber) query.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.status) query.set("status", params.status);
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<InternalRequestSummary>>(accessToken, `/api/v1/requests?${query.toString()}`);
}

export function getInternalRequestById(accessToken: string, internalRequestId: string): Promise<InternalRequestDetail> {
  return apiFetch<InternalRequestDetail>(accessToken, `/api/v1/requests/${internalRequestId}`);
}

export function getMyInternalRequests(accessToken: string): Promise<MyInternalRequestSummary[]> {
  return apiFetch<MyInternalRequestSummary[]>(accessToken, "/api/v1/requests/mine");
}

export function createInternalRequest(
  accessToken: string,
  input: { type: InternalRequestType; assetId: string; justification: string; expectedReturnDate: string | null },
): Promise<string> {
  return apiFetch<string>(accessToken, "/api/v1/requests", { method: "POST", body: JSON.stringify(input) });
}

export function cancelInternalRequest(accessToken: string, internalRequestId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/requests/${internalRequestId}/cancel`, { method: "POST" });
}

// --- Documents (F8) ---

export type DocumentSummary = {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedByUserId: string;
  uploadedByDisplayName: string;
  uploadedAtUtc: string;
};

export function getDocuments(accessToken: string, entityType: string, entityId: string): Promise<DocumentSummary[]> {
  return apiFetch<DocumentSummary[]>(accessToken, `/api/v1/documents?entityType=${entityType}&entityId=${entityId}`);
}

/** Bypasses apiFetch on purpose — a FormData body must set its own multipart Content-Type (with
 * boundary), which apiFetch's unconditional "application/json" default would break. */
export async function uploadDocument(accessToken: string, entityType: string, entityId: string, file: File): Promise<string> {
  const form = new FormData();
  form.set("entityType", entityType);
  form.set("entityId", entityId);
  form.set("file", file);

  const response = await fetch(`${getApiBaseUrl()}/api/v1/documents`, {
    method: "POST",
    cache: "no-store",
    headers: { Authorization: `Bearer ${accessToken}` },
    body: form,
  });

  if (!response.ok) {
    let detail: string | null = null;
    try {
      const problem = (await response.json()) as { detail?: string; title?: string };
      detail = problem.detail ?? problem.title ?? null;
    } catch {
      // No JSON body.
    }
    throw new ApiError(response.status, detail, `API responded with status ${response.status}`);
  }

  return (await response.json()) as string;
}

export function getDocumentDownloadUrl(documentId: string): string {
  return `/api/documents/${documentId}/content`;
}

// --- Notifications (F8) ---

export type MyNotificationSummary = {
  id: string;
  type: string;
  title: string;
  body: string;
  isRead: boolean;
  createdAtUtc: string;
  readAtUtc: string | null;
};

export function getMyNotifications(accessToken: string): Promise<MyNotificationSummary[]> {
  return apiFetch<MyNotificationSummary[]>(accessToken, "/api/v1/notifications/mine");
}

export function markNotificationAsRead(accessToken: string, notificationId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/notifications/${notificationId}/read`, { method: "POST" });
}

// --- Audit (F8) ---

export type AuditEntrySummary = {
  id: string;
  companyId: string | null;
  userId: string | null;
  userDisplayName: string | null;
  commandName: string;
  module: string | null;
  action: string | null;
  detailsJson: string | null;
  succeeded: boolean;
  errorMessage: string | null;
  occurredAtUtc: string;
};

export type AuditSortField = "occurredAtUtc" | "userDisplayName" | "commandName" | "module" | "succeeded";

export function getAuditEntries(
  accessToken: string,
  params: {
    companyId?: string;
    pageNumber?: number;
    pageSize?: number;
    userId?: string;
    commandName?: string;
    fromUtc?: string;
    toUtc?: string;
    sortBy?: AuditSortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<AuditEntrySummary>> {
  const query = new URLSearchParams();
  if (params.companyId) query.set("companyId", params.companyId);
  if (params.pageNumber) query.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.userId) query.set("userId", params.userId);
  if (params.commandName) query.set("commandName", params.commandName);
  if (params.fromUtc) query.set("fromUtc", params.fromUtc);
  if (params.toUtc) query.set("toUtc", params.toUtc);
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<AuditEntrySummary>>(accessToken, `/api/v1/audit-entries?${query.toString()}`);
}

// --- Imports/Exports (F9) ---

export type ImportBatchStatus =
  | "Queued"
  | "Validating"
  | "Validated"
  | "Processing"
  | "Completed"
  | "CompletedWithErrors"
  | "Failed"
  | "Cancelled";

export type ImportCommitMode = "AllOrNothing" | "ValidRowsOnly";

export type ImportBatchSummary = {
  id: string;
  fileName: string;
  status: ImportBatchStatus;
  totalRows: number | null;
  validRows: number | null;
  invalidRows: number | null;
  succeededRows: number | null;
  failedRows: number | null;
  createdAtUtc: string;
};

export type ImportRowResult = {
  rowNumber: number;
  success: boolean;
  errors: string[];
  createdAssetFolio: string | null;
};

export type ImportBatchDetail = {
  id: string;
  companyId: string;
  fileName: string;
  status: ImportBatchStatus;
  commitMode: ImportCommitMode | null;
  totalRows: number | null;
  validRows: number | null;
  invalidRows: number | null;
  succeededRows: number | null;
  failedRows: number | null;
  errorMessage: string | null;
  rows: ImportRowResult[];
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

export type ImportBatchSortField = "createdAtUtc" | "fileName" | "status" | "totalRows";

export function getImportBatches(
  accessToken: string,
  params: {
    companyId: string;
    pageNumber?: number;
    pageSize?: number;
    sortBy?: ImportBatchSortField;
    sortDescending?: boolean;
  },
): Promise<PagedResult<ImportBatchSummary>> {
  const query = new URLSearchParams({ companyId: params.companyId });
  if (params.pageNumber) query.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDescending) query.set("sortDescending", "true");
  return apiFetch<PagedResult<ImportBatchSummary>>(accessToken, `/api/v1/import-batches?${query.toString()}`);
}

export function getImportBatchById(accessToken: string, importBatchId: string): Promise<ImportBatchDetail> {
  return apiFetch<ImportBatchDetail>(accessToken, `/api/v1/import-batches/${importBatchId}`);
}

/** Bypasses apiFetch on purpose, same reason as uploadDocument (F8): a FormData body must set its own
 * multipart Content-Type (with boundary). */
export async function uploadImportBatch(accessToken: string, companyId: string, file: File): Promise<string> {
  const form = new FormData();
  form.set("companyId", companyId);
  form.set("file", file);

  const response = await fetch(`${getApiBaseUrl()}/api/v1/import-batches`, {
    method: "POST",
    cache: "no-store",
    headers: { Authorization: `Bearer ${accessToken}` },
    body: form,
  });

  if (!response.ok) {
    let detail: string | null = null;
    try {
      const problem = (await response.json()) as { detail?: string; title?: string };
      detail = problem.detail ?? problem.title ?? null;
    } catch {
      // No JSON body.
    }
    throw new ApiError(response.status, detail, `API responded with status ${response.status}`);
  }

  return (await response.json()) as string;
}

export function commitImportBatch(accessToken: string, importBatchId: string, mode: ImportCommitMode): Promise<void> {
  return apiFetch(accessToken, `/api/v1/import-batches/${importBatchId}/commit`, {
    method: "POST",
    body: JSON.stringify({ mode }),
  });
}

export function cancelImportBatch(accessToken: string, importBatchId: string): Promise<void> {
  return apiFetch(accessToken, `/api/v1/import-batches/${importBatchId}/cancel`, { method: "POST" });
}

export function getImportTemplateDownloadUrl(assetCategoryId: string): string {
  return `/api/import-batches/template?assetCategoryId=${assetCategoryId}`;
}

export type ExportFileFormat = "Xlsx" | "Pdf";

export function getAssetsExportUrl(params: {
  companyId: string;
  format: ExportFileFormat;
  assetCategoryId?: string;
  status?: AssetStatus;
  search?: string;
}): string {
  const query = new URLSearchParams({ companyId: params.companyId, format: params.format });
  if (params.assetCategoryId) query.set("assetCategoryId", params.assetCategoryId);
  if (params.status) query.set("status", params.status);
  if (params.search) query.set("search", params.search);
  return `/api/exports/assets?${query.toString()}`;
}

// --- Reports (F10) ---

/** `companyId` omitted means consolidated across every company the caller can access
 * (`Reports.ReadConsolidated`); provided means that one company (`Reports.Read`) — see ADR 0012. */
function reportQuery(companyId?: string): URLSearchParams {
  const query = new URLSearchParams();
  if (companyId) query.set("companyId", companyId);
  return query;
}

export type AssetStatusCount = { status: AssetStatus; count: number };

export type AssetCategoryCount = { assetCategoryId: string; assetCategoryName: string; count: number };

export type InventorySummaryResult = {
  totalAssets: number;
  byStatus: AssetStatusCount[];
  byCategory: AssetCategoryCount[];
};

export function getInventorySummary(accessToken: string, companyId?: string): Promise<InventorySummaryResult> {
  return apiFetch<InventorySummaryResult>(accessToken, `/api/v1/reports/inventory-summary?${reportQuery(companyId).toString()}`);
}

export type MaintenanceCategoryKpis = {
  assetCategoryId: string;
  assetCategoryName: string;
  mttrHours: number | null;
  mtbfDays: number | null;
  closedOrdersCount: number;
};

export type MaintenanceKpisResult = {
  mttrHours: number | null;
  mtbfDays: number | null;
  closedOrdersCount: number;
  byCategory: MaintenanceCategoryKpis[];
};

export function getMaintenanceKpis(accessToken: string, companyId?: string): Promise<MaintenanceKpisResult> {
  return apiFetch<MaintenanceKpisResult>(accessToken, `/api/v1/reports/maintenance-kpis?${reportQuery(companyId).toString()}`);
}

export type ExpiringWarrantyRow = {
  warrantyId: string;
  assetId: string;
  assetFolio: string;
  provider: string;
  type: WarrantyType;
  endDate: string;
  daysRemaining: number;
};

export function getExpiringWarranties(accessToken: string, companyId: string | undefined, withinDays = 30): Promise<ExpiringWarrantyRow[]> {
  const query = reportQuery(companyId);
  query.set("withinDays", String(withinDays));
  return apiFetch<ExpiringWarrantyRow[]>(accessToken, `/api/v1/reports/expiring-warranties?${query.toString()}`);
}

export type LowStockConsumableRow = {
  consumableId: string;
  name: string;
  sku: string | null;
  unitOfMeasure: string;
  currentStock: number;
  minimumStock: number;
  shortfall: number;
};

export function getLowStockConsumables(accessToken: string, companyId?: string): Promise<LowStockConsumableRow[]> {
  return apiFetch<LowStockConsumableRow[]>(accessToken, `/api/v1/reports/low-stock-consumables?${reportQuery(companyId).toString()}`);
}

export type ExecutiveDashboardResult = {
  totalAssets: number;
  assignedAssets: number;
  availableAssets: number;
  pendingApprovals: number;
  openMaintenanceOrders: number;
  expiringWarranties: number;
  lowStockConsumables: number;
};

export function getExecutiveDashboard(accessToken: string, companyId?: string): Promise<ExecutiveDashboardResult> {
  return apiFetch<ExecutiveDashboardResult>(accessToken, `/api/v1/dashboards/executive?${reportQuery(companyId).toString()}`);
}

export type ReportExportKey = "expiring-warranties" | "low-stock-consumables" | "maintenance-kpis";

export function getReportExportUrl(
  report: ReportExportKey, format: ExportFileFormat, companyId?: string, withinDays?: number,
): string {
  const query = reportQuery(companyId);
  query.set("report", report);
  query.set("format", format);
  if (withinDays) query.set("withinDays", String(withinDays));
  return `/api/reports/export?${query.toString()}`;
}

// --- Search (F11) ---

export type SearchResultItem = {
  entityType: string;
  entityId: string;
  title: string;
  subtitle: string | null;
  linkEntityType: string;
  linkEntityId: string;
};

export function globalSearch(accessToken: string, params: { term: string; companyId?: string }): Promise<SearchResultItem[]> {
  const query = new URLSearchParams({ term: params.term });
  if (params.companyId) query.set("companyId", params.companyId);
  return apiFetch<SearchResultItem[]>(accessToken, `/api/v1/search?${query.toString()}`);
}
