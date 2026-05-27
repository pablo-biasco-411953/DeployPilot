create table if not exists organizations (
  id uuid primary key,
  name varchar(160) not null,
  slug varchar(120) not null unique,
  created_at timestamptz not null
);

create table if not exists users (
  id uuid primary key,
  organization_id uuid not null references organizations(id),
  display_name varchar(160) not null,
  email varchar(320) not null,
  role varchar(80) not null,
  created_at timestamptz not null
);

create table if not exists repositories (
  id uuid primary key,
  organization_id uuid not null references organizations(id),
  name varchar(160) not null,
  remote_url text not null,
  default_branch varchar(160) not null,
  technology varchar(80) not null,
  project_path text not null,
  build_command text null,
  created_at timestamptz not null
);

create table if not exists applications (
  id uuid primary key,
  organization_id uuid not null references organizations(id),
  name varchar(160) not null,
  slug varchar(120) not null,
  created_at timestamptz not null
);

create table if not exists modules (
  id uuid primary key,
  application_id uuid not null references applications(id),
  name varchar(160) not null,
  executable_name varchar(260) not null,
  install_path text not null,
  is_enabled boolean not null default true,
  created_at timestamptz not null
);

create table if not exists build_templates (
  id uuid primary key,
  name varchar(160) not null,
  technology varchar(80) not null,
  script_path text not null,
  description text not null,
  is_enabled boolean not null default true
);

create table if not exists build_jobs (
  id uuid primary key,
  organization_id uuid not null references organizations(id),
  repository_id uuid not null references repositories(id),
  application_id uuid not null references applications(id),
  module_id uuid not null references modules(id),
  requested_by varchar(160) not null,
  requested_version varchar(64) null,
  requested_sha varchar(64) null,
  status varchar(40) not null,
  progress integer not null,
  requested_at timestamptz not null,
  started_at timestamptz null,
  finished_at timestamptz null
);

create table if not exists build_events (
  id uuid primary key,
  build_job_id uuid not null references build_jobs(id),
  level varchar(40) not null,
  message text not null,
  progress integer not null,
  created_at timestamptz not null
);

create table if not exists versions (
  id uuid primary key,
  module_id uuid not null references modules(id),
  version varchar(64) not null,
  git_sha varchar(64) not null,
  changelog text not null,
  released_at timestamptz not null,
  artifact_id uuid null
);

create table if not exists artifacts (
  id uuid primary key,
  version_id uuid not null references versions(id),
  file_name varchar(260) not null,
  relative_path text not null,
  size_bytes bigint not null,
  sha256 varchar(64) not null,
  created_at timestamptz not null
);

create table if not exists installations (
  id uuid primary key,
  organization_id uuid not null references organizations(id),
  module_id uuid not null references modules(id),
  machine_name varchar(160) not null,
  installed_version varchar(64) not null,
  current_sha varchar(64) null,
  last_seen_at timestamptz not null
);

create table if not exists integrity_checks (
  id uuid primary key,
  installation_id uuid not null references installations(id),
  artifact_id uuid not null references artifacts(id),
  is_valid boolean not null,
  details text not null,
  created_at timestamptz not null
);

create table if not exists agent_nodes (
  id uuid primary key,
  name varchar(160) not null,
  machine_name varchar(160) not null,
  version varchar(64) not null,
  is_online boolean not null,
  last_seen_at timestamptz not null
);

create table if not exists settings (
  key varchar(160) primary key,
  value text not null,
  updated_at timestamptz not null
);
