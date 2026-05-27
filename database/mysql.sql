create table if not exists organizations (
  id char(36) primary key,
  name varchar(160) not null,
  slug varchar(120) not null unique,
  created_at datetime not null
);

create table if not exists users (
  id char(36) primary key,
  organization_id char(36) not null,
  display_name varchar(160) not null,
  email varchar(320) not null,
  role varchar(80) not null,
  created_at datetime not null
);

create table if not exists repositories (
  id char(36) primary key,
  organization_id char(36) not null,
  name varchar(160) not null,
  remote_url text not null,
  default_branch varchar(160) not null,
  technology varchar(80) not null,
  project_path text not null,
  build_command text null,
  created_at datetime not null
);

create table if not exists applications (
  id char(36) primary key,
  organization_id char(36) not null,
  name varchar(160) not null,
  slug varchar(120) not null,
  created_at datetime not null
);

create table if not exists modules (
  id char(36) primary key,
  application_id char(36) not null,
  name varchar(160) not null,
  executable_name varchar(260) not null,
  install_path text not null,
  is_enabled boolean not null default true,
  created_at datetime not null
);

create table if not exists build_templates (
  id char(36) primary key,
  name varchar(160) not null,
  technology varchar(80) not null,
  script_path text not null,
  description text not null,
  is_enabled boolean not null default true
);

create table if not exists build_jobs (
  id char(36) primary key,
  organization_id char(36) not null,
  repository_id char(36) not null,
  application_id char(36) not null,
  module_id char(36) not null,
  requested_by varchar(160) not null,
  requested_version varchar(64) null,
  requested_sha varchar(64) null,
  status varchar(40) not null,
  progress int not null,
  requested_at datetime not null,
  started_at datetime null,
  finished_at datetime null
);

create table if not exists build_events (
  id char(36) primary key,
  build_job_id char(36) not null,
  level varchar(40) not null,
  message text not null,
  progress int not null,
  created_at datetime not null
);

create table if not exists versions (
  id char(36) primary key,
  module_id char(36) not null,
  version varchar(64) not null,
  git_sha varchar(64) not null,
  changelog text not null,
  released_at datetime not null,
  artifact_id char(36) null
);

create table if not exists artifacts (
  id char(36) primary key,
  version_id char(36) not null,
  file_name varchar(260) not null,
  relative_path text not null,
  size_bytes bigint not null,
  sha256 varchar(64) not null,
  created_at datetime not null
);

create table if not exists installations (
  id char(36) primary key,
  organization_id char(36) not null,
  module_id char(36) not null,
  machine_name varchar(160) not null,
  installed_version varchar(64) not null,
  current_sha varchar(64) null,
  last_seen_at datetime not null
);

create table if not exists integrity_checks (
  id char(36) primary key,
  installation_id char(36) not null,
  artifact_id char(36) not null,
  is_valid boolean not null,
  details text not null,
  created_at datetime not null
);

create table if not exists agent_nodes (
  id char(36) primary key,
  name varchar(160) not null,
  machine_name varchar(160) not null,
  version varchar(64) not null,
  is_online boolean not null,
  last_seen_at datetime not null
);

create table if not exists settings (
  `key` varchar(160) primary key,
  value text not null,
  updated_at datetime not null
);
