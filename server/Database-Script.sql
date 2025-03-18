create table companys
(
    id   serial
        constraint companys_pk
            primary key,
    name varchar
        constraint companys_pk_2
            unique
);

alter table companys
    owner to postgres;

create table users
(
    id        serial
        constraint users_pk
            primary key,
    username  varchar
        constraint users_pk_2
            unique,
    password  varchar,
    role      role,
    email     varchar
        constraint users_pk_3
            unique,
    company   integer not null
        constraint users_companys_id_fk
            references companys,
    firstname varchar,
    lastname  varchar
);

alter table users
    owner to postgres;

create table issues
(
    id             serial
        constraint issues_pk
            primary key,
    company_id     integer
        constraint issues_companys_id_fk
            references companys,
    customer_email varchar     not null,
    subject        varchar,
    state          issue_state not null,
    title          varchar
);

alter table issues
    owner to postgres;

create table messages
(
    id       serial
        constraint messages_pk
            primary key,
    issue_id integer
        constraint messages_issues_id_fk
            references issues,
    message  text,
    sender   sender
);

alter table messages
    owner to postgres;

create table subjects
(
    id         serial
        constraint subjects_pk
            primary key,
    company_id integer,
    name       varchar,
    constraint subjects_pk_2
        unique (company_id, name)
);

alter table subjects
    owner to postgres;

INSERT INTO public.companys (name) VALUES ('Demo AB');
INSERT INTO public.companys (name) VALUES ('Test AB');

INSERT INTO public.users (firstname, lastname, username, password, role, email, company) VALUES ( 'Admin', 'Adminsson','Master', 'abc123', 'ADMIN', 'm@email.com', 1);
INSERT INTO public.users (firstname, lastname, username, password, role, email, company) VALUES ( 'Linus', 'Lindroth','no92one', 'abc123', 'USER', 'no@email.com', 1);
INSERT INTO public.users (firstname, lastname, username, password, role, email, company) VALUES ( 'Testaren', 'Testsson','Testare', 'abc123', 'ADMIN', 'test@gmail.com', 2);

INSERT INTO public.subjects (company_id, name) VALUES (1, 'Reklamation');
INSERT INTO public.subjects (company_id, name) VALUES (1, 'Skada');
INSERT INTO public.subjects (company_id, name) VALUES (1, 'Övrigt');
