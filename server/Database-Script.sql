create type role as enum ('USER', 'ADMIN');

alter type role owner to postgres;

create type issue_state as enum ('CLOSED', 'OPEN', 'NEW');

alter type issue_state owner to postgres;

create type sender as enum ('CUSTOMER', 'SUPPORT', 'BOT');

alter type sender owner to postgres;

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
    issue_id integer not null
        constraint messages_issues_id_fk
            references issues,
    message  text    not null,
    sender   sender  not null,
    username varchar not null
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

create view users_with_company
            (user_id, firstname, lastname, username, password, email, role, company_id, company_name) as
SELECT u.id   AS user_id,
       u.firstname,
       u.lastname,
       u.username,
       u.password,
       u.email,
       u.role,
       c.id   AS company_id,
       c.name AS company_name
FROM users u
         JOIN companys c ON u.company = c.id;

alter table users_with_company
    owner to postgres;

create view customers_issue_information(id, company_name, customer_email, subject, state, title) as
SELECT i.id,
       c.name AS company_name,
       i.customer_email,
       i.subject,
       i.state,
       i.title
FROM issues i
         JOIN companys c ON i.company_id = c.id;

alter table customers_issue_information
    owner to postgres;

create view issue_messages(id, issue_id, message, sender, username) as
SELECT m.id,
       m.issue_id,
       m.message,
       m.sender,
       m.username
FROM messages m
         JOIN issues i ON m.issue_id = i.id
ORDER BY m.id;

alter table issue_messages
    owner to postgres;

INSERT INTO companys (name) VALUES ('Demo AB');
INSERT INTO companys (name) VALUES ('Test AB');

INSERT INTO users (firstname, lastname, username, password, role, email, company) VALUES ( 'Admin', 'Adminsson','Master', 'abc123', 'ADMIN', 'm@email.com', 1);
INSERT INTO users (firstname, lastname, username, password, role, email, company) VALUES ( 'Linus', 'Lindroth','no92one', 'abc123', 'USER', 'no@email.com', 1);
INSERT INTO users (firstname, lastname, username, password, role, email, company) VALUES ( 'Testaren', 'Testsson','Testare', 'abc123', 'ADMIN', 'test@gmail.com', 2);

INSERT INTO subjects (company_id, name) VALUES (1, 'Reklamation');
INSERT INTO subjects (company_id, name) VALUES (1, 'Skada');
INSERT INTO subjects (company_id, name) VALUES (1, 'Övrigt');

INSERT INTO issues (company_id, customer_email, subject, state, title) VALUES (1, 'Linus@email.test', 'Test', 'NEW', 'Test Issue');
INSERT INTO issues (company_id, customer_email, subject, state, title) VALUES ( 1, 'linus.lindroth.92@gmail.com', 'Övrigt', 'NEW', 'Test Issue');

INSERT INTO messages (issue_id, message, sender, username) VALUES (2, 'Some message.', 'CUSTOMER', 'Linus@email.test');
INSERT INTO messages (issue_id, message, sender, username) VALUES (3, 'This is just a test.', 'CUSTOMER', 'linus.lindroth.92@gmail.com');
INSERT INTO messages (issue_id, message, sender, username) VALUES (2, ' Lorem ipsum dolor sit amet consectetur adipisicing elit. Quo corporis nemo error provident eligendi consequuntur cum aliquam placeat aspernatur amet dolore ut quasi impedit culpa laboriosam suscipit, nobis natus nesciunt. Aliquam exercitationem facere in cupiditate voluptates voluptatum, perspiciatis est quidem dolores veniam magnam atque vitae. Rerum aut id delectus debitis exercitationem eos harum perspiciatis, voluptatem tenetur officiis libero aliquam iste. Repellendus autem placeat hic odit dignissimos. Blanditiis odio, facilis sequi ratione repudiandae iusto, distinctio reiciendis consectetur deleniti eius fugit numquam laborum nobis quasi magnam cupiditate laudantium illo, provident labore. Impedit.', 'CUSTOMER', 'Linus@email.test');
