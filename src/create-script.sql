CREATE TABLE IF NOT EXISTS PEOPLE (
	ID UUID PRIMARY KEY,
	APELIDO VARCHAR(32) UNIQUE NOT NULL,
	NOME VARCHAR(100) NOT NULL,
	NASCIMENTO DATE NOT NULL,
	STACK JSONB NULL
);