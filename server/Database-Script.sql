create type role as enum ('USER', 'ADMIN');

alter type role owner to postgres;

create table users
(
    id       serial
        constraint users_pk
            primary key,
    username varchar
        constraint users_pk_2
            unique,
    password varchar,
    role     role
);

alter table users
    owner to postgres;

INSERT INTO public.users (id, username, password, role) VALUES (1, 'no92one', 'abc123', 'USER');
INSERT INTO public.users (id, username, password, role) VALUES (2, 'Master', 'abc123', 'ADMIN');
