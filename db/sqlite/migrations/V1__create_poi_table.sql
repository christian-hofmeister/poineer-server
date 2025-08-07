CREATE TABLE IF NOT EXISTS poi (
    id TEXT PRIMARY KEY,
    name TEXT,
    lat REAL,
    lon REAL,
    type TEXT,
    subtype TEXT,
    tags TEXT
);
