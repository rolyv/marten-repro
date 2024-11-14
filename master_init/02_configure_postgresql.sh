#!/bin/bash
echo "wal_level = replica" >> $PGDATA/postgresql.conf
echo "max_wal_senders = 10" >> $PGDATA/postgresql.conf
echo "wal_keep_size = 64MB" >> $PGDATA/postgresql.conf
echo "listen_addresses = '*'" >> $PGDATA/postgresql.conf

