# marten-repro


Run `docker compose up` to spin up the master-replica postgres setup.


The only way I was able to reproduce the issue was when I set the read replica as the first host in the connection string.
If I set master as the first host, I was unable to reproduce the issue.


Stacktrace of exception that I encountered:

```
2024-11-14T09:15:25.0554910 fail: Microsoft.Extensions.Hosting.Internal.Host[11]
      Hosting failed to start
      Marten.Exceptions.MartenSchemaException: DDL Execution for 'All Configured Changes' Failed!
      
      CREATE
      OR REPLACE FUNCTION public.mt_immutable_timestamp(value text) RETURNS timestamp without time zone LANGUAGE sql IMMUTABLE AS
      $function$
      select value::timestamp
      
      $function$;
      
      
      CREATE
      OR REPLACE FUNCTION public.mt_immutable_timestamptz(value text) RETURNS timestamp with time zone LANGUAGE sql IMMUTABLE AS
      $function$
      select value::timestamptz
      
      $function$;
      
      
      CREATE
      OR REPLACE FUNCTION public.mt_immutable_time(value text) RETURNS time without time zone LANGUAGE sql IMMUTABLE AS
      $function$
      select value::time
      
      $function$;
      
      
      CREATE
      OR REPLACE FUNCTION public.mt_immutable_date(value text) RETURNS date LANGUAGE sql IMMUTABLE AS
      $function$
      select value::date
      
      $function$;
      
      
      CREATE
      OR REPLACE FUNCTION public.mt_grams_vector(text)
              RETURNS tsvector
              LANGUAGE plpgsql
              IMMUTABLE STRICT
      AS $function$
      BEGIN
      RETURN (SELECT array_to_string(public.mt_grams_array($1), ' ') ::tsvector);
      END
      $function$;
      
      
      CREATE
      OR REPLACE FUNCTION public.mt_grams_query(text)
              RETURNS tsquery
              LANGUAGE plpgsql
              IMMUTABLE STRICT
      AS $function$
      BEGIN
      RETURN (SELECT array_to_string(public.mt_grams_array($1), ' & ') ::tsquery);
      END
      $function$;
      
      
      CREATE
      OR REPLACE FUNCTION public.mt_grams_array(words text)
              RETURNS text[]
              LANGUAGE plpgsql
              IMMUTABLE STRICT
      AS $function$
              DECLARE
      result text[];
              DECLARE
      word text;
              DECLARE
      clean_word text;
      BEGIN
                      FOREACH
      word IN ARRAY string_to_array(words, ' ')
                      LOOP
                           clean_word = regexp_replace(word, '[^a-zA-Z0-9]+', '','g');
      FOR i IN 1 .. length(clean_word)
                           LOOP
                               result := result || quote_literal(substr(lower(clean_word), i, 1));
                               result
      := result || quote_literal(substr(lower(clean_word), i, 2));
                               result
      := result || quote_literal(substr(lower(clean_word), i, 3));
      END LOOP;
      END LOOP;
      
      RETURN ARRAY(SELECT DISTINCT e FROM unnest(result) AS a(e) ORDER BY e);
      END;
      $function$;
      
      
      CREATE OR REPLACE FUNCTION public.mt_jsonb_append(jsonb, text[], jsonb, boolean)
          RETURNS jsonb
          LANGUAGE plpgsql
      AS $function$
      DECLARE
          retval ALIAS FOR $1;
          location ALIAS FOR $2;
          val ALIAS FOR $3;
          if_not_exists ALIAS FOR $4;
          tmp_value jsonb;
      BEGIN
          tmp_value = retval #> location;
          IF tmp_value IS NOT NULL AND jsonb_typeof(tmp_value) = 'array' THEN
              CASE
                  WHEN NOT if_not_exists THEN
                      retval = jsonb_set(retval, location, tmp_value || val, FALSE);
                  WHEN jsonb_typeof(val) = 'object' AND NOT tmp_value @> jsonb_build_array(val) THEN
                      retval = jsonb_set(retval, location, tmp_value || val, FALSE);
                  WHEN jsonb_typeof(val) <> 'object' AND NOT tmp_value @> val THEN
                      retval = jsonb_set(retval, location, tmp_value || val, FALSE);
                  ELSE NULL;
                  END CASE;
          END IF;
          RETURN retval;
      END;
      $function$;
      
      
      CREATE OR REPLACE FUNCTION public.mt_jsonb_copy(jsonb, text[], text[])
          RETURNS jsonb
          LANGUAGE plpgsql
      AS $function$
      DECLARE
          retval ALIAS FOR $1;
          src_path ALIAS FOR $2;
          dst_path ALIAS FOR $3;
          tmp_value jsonb;
      BEGIN
          tmp_value = retval #> src_path;
          retval = public.mt_jsonb_fix_null_parent(retval, dst_path);
          RETURN jsonb_set(retval, dst_path, tmp_value::jsonb, TRUE);
      END;
      $function$;
      
      
      CREATE OR REPLACE FUNCTION public.mt_jsonb_duplicate(jsonb, text[], jsonb)
      RETURNS jsonb
      LANGUAGE plpgsql
      AS $function$
      DECLARE
          retval ALIAS FOR $1;
          location ALIAS FOR $2;
          targets ALIAS FOR $3;
          tmp_value jsonb;
          target_path text[];
          target text;
      BEGIN
          FOR target IN SELECT jsonb_array_elements_text(targets)
          LOOP
              target_path = public.mt_jsonb_path_to_array(target, '\.');
              retval = public.mt_jsonb_copy(retval, location, target_path);
          END LOOP;
      
          RETURN retval;
      END;
      $function$;
      
      
      CREATE OR REPLACE FUNCTION public.mt_jsonb_fix_null_parent(jsonb, text[])
          RETURNS jsonb
          LANGUAGE plpgsql
      AS $function$
      DECLARE
      retval ALIAS FOR $1;
          dst_path ALIAS FOR $2;
          dst_path_segment text[] = ARRAY[]::text[];
          dst_path_array_length integer;
          i integer = 1;
      BEGIN
          dst_path_array_length = array_length(dst_path, 1);
          WHILE i <=(dst_path_array_length - 1)
          LOOP
              dst_path_segment = dst_path_segment || ARRAY[dst_path[i]];
              IF retval #> dst_path_segment = 'null'::jsonb THEN
                  retval = jsonb_set(retval, dst_path_segment, '{}'::jsonb, TRUE);
              END IF;
              i = i + 1;
          END LOOP;
      
          RETURN retval;
      END;
      $function$;
      
      
      CREATE OR REPLACE FUNCTION public.mt_jsonb_increment(jsonb, text[], numeric)
          RETURNS jsonb
          LANGUAGE plpgsql
      AS $function$
      DECLARE
      retval ALIAS FOR $1;
          location ALIAS FOR $2;
          increment_value ALIAS FOR $3;
          tmp_value jsonb;
      BEGIN
          tmp_value = retval #> location;
          IF tmp_value IS NULL THEN
              tmp_value = to_jsonb(0);
      END IF;
      
      RETURN jsonb_set(retval, location, to_jsonb(tmp_value::numeric + increment_value), TRUE);
      END;
      $function$;
      
      
      CREATE OR REPLACE FUNCTION public.mt_jsonb_insert(jsonb, text[], jsonb, integer, boolean)
          RETURNS jsonb
          LANGUAGE plpgsql
      AS $function$
      DECLARE
          retval ALIAS FOR $1;
          location ALIAS FOR $2;
          val ALIAS FOR $3;
          elm_index ALIAS FOR $4;
          if_not_exists ALIAS FOR $5;
          tmp_value jsonb;
      BEGIN
          tmp_value = retval #> location;
          IF tmp_value IS NOT NULL AND jsonb_typeof(tmp_value) = 'array' THEN
              IF elm_index IS NULL THEN
                  elm_index = jsonb_array_length(tmp_value) + 1;
              END IF;
              CASE
                  WHEN NOT if_not_exists THEN
                      retval = jsonb_insert(retval, location || elm_index::text, val);
                  WHEN jsonb_typeof(val) = 'object' AND NOT tmp_value @> jsonb_build_array(val) THEN
                      retval = jsonb_insert(retval, location || elm_index::text, val);
                  WHEN jsonb_typeof(val) <> 'object' AND NOT tmp_value @> val THEN
                      retval = jsonb_insert(retval, location || elm_index::text, val);
                  ELSE NULL;
              END CASE;
          END IF;
          RETURN retval;
      END;
      $function$;
      
      
      CREATE OR REPLACE FUNCTION public.mt_jsonb_move(jsonb, text[], text)
          RETURNS jsonb
          LANGUAGE plpgsql
      AS $function$
      DECLARE
          retval ALIAS FOR $1;
          src_path ALIAS FOR $2;
          dst_name ALIAS FOR $3;
          dst_path text[];
          tmp_value jsonb;
      BEGIN
          tmp_value = retval #> src_path;
          retval = retval #- src_path;
          dst_path = src_path;
          dst_path[array_length(dst_path, 1)] = dst_name;
          retval = public.mt_jsonb_fix_null_parent(retval, dst_path);
          RETURN jsonb_set(retval, dst_path, tmp_value, TRUE);
      END;
      $function$;
      
      
      CREATE OR REPLACE FUNCTION public.mt_jsonb_path_to_array(text, character)
          RETURNS text[]
          LANGUAGE plpgsql
      AS $function$
      DECLARE
          location ALIAS FOR $1;
          regex_pattern ALIAS FOR $2;
      BEGIN
      RETURN regexp_split_to_array(location, regex_pattern)::text[];
      END;
      $function$;
      
      
      CREATE OR REPLACE FUNCTION public.mt_jsonb_remove(jsonb, text[], jsonb)
          RETURNS jsonb
          LANGUAGE plpgsql
      AS $function$
      DECLARE
          retval ALIAS FOR $1;
          location ALIAS FOR $2;
          val ALIAS FOR $3;
          tmp_value jsonb;
      BEGIN
          tmp_value = retval #> location;
          IF tmp_value IS NOT NULL AND jsonb_typeof(tmp_value) = 'array' THEN
              tmp_value =(SELECT jsonb_agg(elem)
              FROM jsonb_array_elements(tmp_value) AS elem
              WHERE elem <> val);
      
              IF tmp_value IS NULL THEN
                  tmp_value = '[]'::jsonb;
              END IF;
          END IF;
          RETURN jsonb_set(retval, location, tmp_value, FALSE);
      END;
      $function$;
      
      
      CREATE OR REPLACE FUNCTION public.mt_jsonb_patch(jsonb, jsonb)
          RETURNS jsonb
          LANGUAGE plpgsql
      AS $function$
      DECLARE
          retval ALIAS FOR $1;
          patchset ALIAS FOR $2;
          patch jsonb;
          patch_path text[];
          value jsonb;
      BEGIN
          FOR patch IN SELECT * from jsonb_array_elements(patchset)
          LOOP
              patch_path = public.mt_jsonb_path_to_array((patch->>'path')::text, '\.');
      
              CASE patch->>'type'
                  WHEN 'set' THEN
                      retval = jsonb_set(retval, patch_path,(patch->'value')::jsonb, TRUE);
              WHEN 'delete' THEN
                      retval = retval#-patch_path;
              WHEN 'append' THEN
                      retval = public.mt_jsonb_append(retval, patch_path,(patch->'value')::jsonb, FALSE);
              WHEN 'append_if_not_exists' THEN
                      retval = public.mt_jsonb_append(retval, patch_path,(patch->'value')::jsonb, TRUE);
              WHEN 'insert' THEN
                      retval = public.mt_jsonb_insert(retval, patch_path,(patch->'value')::jsonb,(patch->>'index')::integer, FALSE);
              WHEN 'insert_if_not_exists' THEN
                      retval = public.mt_jsonb_insert(retval, patch_path,(patch->'value')::jsonb,(patch->>'index')::integer, TRUE);
              WHEN 'remove' THEN
                      retval = public.mt_jsonb_remove(retval, patch_path,(patch->'value')::jsonb);
              WHEN 'duplicate' THEN
                      retval = public.mt_jsonb_duplicate(retval, patch_path,(patch->'targets')::jsonb);
              WHEN 'rename' THEN
                      retval = public.mt_jsonb_move(retval, patch_path,(patch->>'to')::text);
              WHEN 'increment' THEN
                      retval = public.mt_jsonb_increment(retval, patch_path,(patch->>'increment')::numeric);
              WHEN 'increment_float' THEN
                      retval = public.mt_jsonb_increment(retval, patch_path,(patch->>'increment')::numeric);
              ELSE NULL;
              END CASE;
          END LOOP;
          RETURN retval;
      END;
      $function$;
      
      
      DROP TABLE IF EXISTS public.mt_doc_weatherforecast2 CASCADE;
      CREATE TABLE public.mt_doc_weatherforecast2 (
          id                  uuid                        NOT NULL,
          data                jsonb                       NOT NULL,
          mt_last_modified    timestamp with time zone    NULL DEFAULT (transaction_timestamp()),
          mt_version          uuid                        NOT NULL DEFAULT (md5(random()::text || clock_timestamp()::text)::uuid),
          mt_dotnet_type      varchar                     NULL,
      CONSTRAINT pkey_mt_doc_weatherforecast2_id PRIMARY KEY (id)
      );
      
      CREATE OR REPLACE FUNCTION public.mt_upsert_weatherforecast2(doc JSONB, docDotNetType varchar, docId uuid, docVersion uuid) RETURNS UUID LANGUAGE plpgsql SECURITY INVOKER AS $function$
      DECLARE
        final_version uuid;
      BEGIN
      INSERT INTO public.mt_doc_weatherforecast2 ("data", "mt_dotnet_type", "id", "mt_version", mt_last_modified) VALUES (doc, docDotNetType, docId, docVersion, transaction_timestamp())
        ON CONFLICT (id)
        DO UPDATE SET "data" = doc, "mt_dotnet_type" = docDotNetType, "mt_version" = docVersion, mt_last_modified = transaction_timestamp();
      
        SELECT mt_version FROM public.mt_doc_weatherforecast2 into final_version WHERE id = docId ;
        RETURN final_version;
      END;
      $function$;
      
      
      CREATE OR REPLACE FUNCTION public.mt_insert_weatherforecast2(doc JSONB, docDotNetType varchar, docId uuid, docVersion uuid) RETURNS UUID LANGUAGE plpgsql SECURITY INVOKER AS $function$
      BEGIN
      INSERT INTO public.mt_doc_weatherforecast2 ("data", "mt_dotnet_type", "id", "mt_version", mt_last_modified) VALUES (doc, docDotNetType, docId, docVersion, transaction_timestamp());
      
        RETURN docVersion;
      END;
      $function$;
      
      
      CREATE OR REPLACE FUNCTION public.mt_update_weatherforecast2(doc JSONB, docDotNetType varchar, docId uuid, docVersion uuid) RETURNS UUID LANGUAGE plpgsql SECURITY INVOKER AS $function$
      DECLARE
        final_version uuid;
      BEGIN
        UPDATE public.mt_doc_weatherforecast2 SET "data" = doc, "mt_dotnet_type" = docDotNetType, "mt_version" = docVersion, mt_last_modified = transaction_timestamp() where id = docId;
      
        SELECT mt_version FROM public.mt_doc_weatherforecast2 into final_version WHERE id = docId ;
        RETURN final_version;
      END;
      $function$;
      
      
       ---> Npgsql.PostgresException (0x80004005): 25006: cannot execute CREATE FUNCTION in a read-only transaction
         at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)
         at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)
         at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
         at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
         at Npgsql.NpgsqlCommand.ExecuteNonQuery(Boolean async, CancellationToken cancellationToken)
         at Weasel.Postgresql.PostgresqlMigrator.executeDelta(SchemaMigration migration, DbConnection conn, AutoCreate autoCreate, IMigrationLogger logger, CancellationToken ct)
        Exception data:
          Severity: ERROR
          SqlState: 25006
          MessageText: cannot execute CREATE FUNCTION in a read-only transaction
          File: utility.c
          Line: 414
          Routine: PreventCommandIfReadOnly
         --- End of inner exception stack trace ---
         at Marten.StoreOptions.Weasel.Core.Migrations.IMigrationLogger.OnFailure(DbCommand command, Exception ex)
         at Weasel.Postgresql.PostgresqlMigrator.executeDelta(SchemaMigration migration, DbConnection conn, AutoCreate autoCreate, IMigrationLogger logger, CancellationToken ct)
         at Weasel.Core.Migrations.DatabaseBase`1.ApplyAllConfiguredChangesToDatabaseAsync(IGlobalLock`1 globalLock, Nullable`1 override, ReconnectionOptions reconnectionOptions, CancellationToken ct)
         at Weasel.Core.Migrations.DatabaseBase`1.ApplyAllConfiguredChangesToDatabaseAsync(IGlobalLock`1 globalLock, Nullable`1 override, ReconnectionOptions reconnectionOptions, CancellationToken ct)
         at Marten.Services.MartenActivator.StartAsync(CancellationToken cancellationToken)
         at Microsoft.Extensions.Hosting.Internal.Host.<StartAsync>b__15_1(IHostedService service, CancellationToken token)
         at Microsoft.Extensions.Hosting.Internal.Host.ForeachService[T](IEnumerable`1 services, CancellationToken token, Boolean concurrent, Boolean abortOnFirstException, List`1 exceptions, Func`3 operation)
2024-11-14T09:15:25.1687000 info: Npgsql.Command[2001]
      Batch execution completed (duration=2ms): [update public.wolverine_incoming_envelopes set owner_id = 0 where owner_id = $1, update public.wolverine_outgoing_envelopes set owner_id = 0 where owner_id = $1]
ERROR:
Marten.Exceptions.MartenSchemaException: DDL Execution for 'All Configured Changes' Failed!

CREATE
OR REPLACE FUNCTION public.mt_immutable_timestamp(value text) RETURNS timestamp without time zone LANGUAGE sql IMMUTABLE AS
$function$
select value::timestamp

$function$;


CREATE
OR REPLACE FUNCTION public.mt_immutable_timestamptz(value text) RETURNS timestamp with time zone LANGUAGE sql IMMUTABLE AS
$function$
select value::timestamptz

$function$;


CREATE
OR REPLACE FUNCTION public.mt_immutable_time(value text) RETURNS time without time zone LANGUAGE sql IMMUTABLE AS
$function$
select value::time

$function$;


CREATE
OR REPLACE FUNCTION public.mt_immutable_date(value text) RETURNS date LANGUAGE sql IMMUTABLE AS
$function$
select value::date

$function$;


CREATE
OR REPLACE FUNCTION public.mt_grams_vector(text)
        RETURNS tsvector
        LANGUAGE plpgsql
        IMMUTABLE STRICT
AS $function$
BEGIN
RETURN (SELECT array_to_string(public.mt_grams_array($1), ' ') ::tsvector);
END
$function$;


CREATE
OR REPLACE FUNCTION public.mt_grams_query(text)
        RETURNS tsquery
        LANGUAGE plpgsql
        IMMUTABLE STRICT
AS $function$
BEGIN
RETURN (SELECT array_to_string(public.mt_grams_array($1), ' & ') ::tsquery);
END
$function$;


CREATE
OR REPLACE FUNCTION public.mt_grams_array(words text)
        RETURNS text[]
        LANGUAGE plpgsql
        IMMUTABLE STRICT
AS $function$
        DECLARE
result text[];
        DECLARE
word text;
        DECLARE
clean_word text;
BEGIN
                FOREACH
word IN ARRAY string_to_array(words, ' ')
                LOOP
                     clean_word = regexp_replace(word, '[^a-zA-Z0-9]+', '','g');
FOR i IN 1 .. length(clean_word)
                     LOOP
                         result := result || quote_literal(substr(lower(clean_word), i, 1));
                         result
:= result || quote_literal(substr(lower(clean_word), i, 2));
                         result
:= result || quote_literal(substr(lower(clean_word), i, 3));
END LOOP;
END LOOP;

RETURN ARRAY(SELECT DISTINCT e FROM unnest(result) AS a(e) ORDER BY e);
END;
$function$;


CREATE OR REPLACE FUNCTION public.mt_jsonb_append(jsonb, text[], jsonb, boolean)
    RETURNS jsonb
    LANGUAGE plpgsql
AS $function$
DECLARE
    retval ALIAS FOR $1;
    location ALIAS FOR $2;
    val ALIAS FOR $3;
    if_not_exists ALIAS FOR $4;
    tmp_value jsonb;
BEGIN
    tmp_value = retval #> location;
    IF tmp_value IS NOT NULL AND jsonb_typeof(tmp_value) = 'array' THEN
        CASE
            WHEN NOT if_not_exists THEN
                retval = jsonb_set(retval, location, tmp_value || val, FALSE);
            WHEN jsonb_typeof(val) = 'object' AND NOT tmp_value @> jsonb_build_array(val) THEN
                retval = jsonb_set(retval, location, tmp_value || val, FALSE);
            WHEN jsonb_typeof(val) <> 'object' AND NOT tmp_value @> val THEN
                retval = jsonb_set(retval, location, tmp_value || val, FALSE);
            ELSE NULL;
            END CASE;
    END IF;
    RETURN retval;
END;
$function$;


CREATE OR REPLACE FUNCTION public.mt_jsonb_copy(jsonb, text[], text[])
    RETURNS jsonb
    LANGUAGE plpgsql
AS $function$
DECLARE
    retval ALIAS FOR $1;
    src_path ALIAS FOR $2;
    dst_path ALIAS FOR $3;
    tmp_value jsonb;
BEGIN
    tmp_value = retval #> src_path;
    retval = public.mt_jsonb_fix_null_parent(retval, dst_path);
    RETURN jsonb_set(retval, dst_path, tmp_value::jsonb, TRUE);
END;
$function$;


CREATE OR REPLACE FUNCTION public.mt_jsonb_duplicate(jsonb, text[], jsonb)
RETURNS jsonb
LANGUAGE plpgsql
AS $function$
DECLARE
    retval ALIAS FOR $1;
    location ALIAS FOR $2;
    targets ALIAS FOR $3;
    tmp_value jsonb;
    target_path text[];
    target text;
BEGIN
    FOR target IN SELECT jsonb_array_elements_text(targets)
    LOOP
        target_path = public.mt_jsonb_path_to_array(target, '\.');
        retval = public.mt_jsonb_copy(retval, location, target_path);
    END LOOP;

    RETURN retval;
END;
$function$;


CREATE OR REPLACE FUNCTION public.mt_jsonb_fix_null_parent(jsonb, text[])
    RETURNS jsonb
    LANGUAGE plpgsql
AS $function$
DECLARE
retval ALIAS FOR $1;
    dst_path ALIAS FOR $2;
    dst_path_segment text[] = ARRAY[]::text[];
    dst_path_array_length integer;
    i integer = 1;
BEGIN
    dst_path_array_length = array_length(dst_path, 1);
    WHILE i <=(dst_path_array_length - 1)
    LOOP
        dst_path_segment = dst_path_segment || ARRAY[dst_path[i]];
        IF retval #> dst_path_segment = 'null'::jsonb THEN
            retval = jsonb_set(retval, dst_path_segment, '{}'::jsonb, TRUE);
        END IF;
        i = i + 1;
    END LOOP;

    RETURN retval;
END;
$function$;


CREATE OR REPLACE FUNCTION public.mt_jsonb_increment(jsonb, text[], numeric)
    RETURNS jsonb
    LANGUAGE plpgsql
AS $function$
DECLARE
retval ALIAS FOR $1;
    location ALIAS FOR $2;
    increment_value ALIAS FOR $3;
    tmp_value jsonb;
BEGIN
    tmp_value = retval #> location;
    IF tmp_value IS NULL THEN
        tmp_value = to_jsonb(0);
END IF;

RETURN jsonb_set(retval, location, to_jsonb(tmp_value::numeric + increment_value), TRUE);
END;
$function$;


CREATE OR REPLACE FUNCTION public.mt_jsonb_insert(jsonb, text[], jsonb, integer, boolean)
    RETURNS jsonb
    LANGUAGE plpgsql
AS $function$
DECLARE
    retval ALIAS FOR $1;
    location ALIAS FOR $2;
    val ALIAS FOR $3;
    elm_index ALIAS FOR $4;
    if_not_exists ALIAS FOR $5;
    tmp_value jsonb;
BEGIN
    tmp_value = retval #> location;
    IF tmp_value IS NOT NULL AND jsonb_typeof(tmp_value) = 'array' THEN
        IF elm_index IS NULL THEN
            elm_index = jsonb_array_length(tmp_value) + 1;
        END IF;
        CASE
            WHEN NOT if_not_exists THEN
                retval = jsonb_insert(retval, location || elm_index::text, val);
            WHEN jsonb_typeof(val) = 'object' AND NOT tmp_value @> jsonb_build_array(val) THEN
                retval = jsonb_insert(retval, location || elm_index::text, val);
            WHEN jsonb_typeof(val) <> 'object' AND NOT tmp_value @> val THEN
                retval = jsonb_insert(retval, location || elm_index::text, val);
            ELSE NULL;
        END CASE;
    END IF;
    RETURN retval;
END;
$function$;


CREATE OR REPLACE FUNCTION public.mt_jsonb_move(jsonb, text[], text)
    RETURNS jsonb
    LANGUAGE plpgsql
AS $function$
DECLARE
    retval ALIAS FOR $1;
    src_path ALIAS FOR $2;
    dst_name ALIAS FOR $3;
    dst_path text[];
    tmp_value jsonb;
BEGIN
    tmp_value = retval #> src_path;
    retval = retval #- src_path;
    dst_path = src_path;
    dst_path[array_length(dst_path, 1)] = dst_name;
    retval = public.mt_jsonb_fix_null_parent(retval, dst_path);
    RETURN jsonb_set(retval, dst_path, tmp_value, TRUE);
END;
$function$;


CREATE OR REPLACE FUNCTION public.mt_jsonb_path_to_array(text, character)
    RETURNS text[]
    LANGUAGE plpgsql
AS $function$
DECLARE
    location ALIAS FOR $1;
    regex_pattern ALIAS FOR $2;
BEGIN
RETURN regexp_split_to_array(location, regex_pattern)::text[];
END;
$function$;


CREATE OR REPLACE FUNCTION public.mt_jsonb_remove(jsonb, text[], jsonb)
    RETURNS jsonb
    LANGUAGE plpgsql
AS $function$
DECLARE
    retval ALIAS FOR $1;
    location ALIAS FOR $2;
    val ALIAS FOR $3;
    tmp_value jsonb;
BEGIN
    tmp_value = retval #> location;
    IF tmp_value IS NOT NULL AND jsonb_typeof(tmp_value) = 'array' THEN
        tmp_value =(SELECT jsonb_agg(elem)
        FROM jsonb_array_elements(tmp_value) AS elem
        WHERE elem <> val);

        IF tmp_value IS NULL THEN
            tmp_value = '[]'::jsonb;
        END IF;
    END IF;
    RETURN jsonb_set(retval, location, tmp_value, FALSE);
END;
$function$;


CREATE OR REPLACE FUNCTION public.mt_jsonb_patch(jsonb, jsonb)
    RETURNS jsonb
    LANGUAGE plpgsql
AS $function$
DECLARE
    retval ALIAS FOR $1;
    patchset ALIAS FOR $2;
    patch jsonb;
    patch_path text[];
    value jsonb;
BEGIN
    FOR patch IN SELECT * from jsonb_array_elements(patchset)
    LOOP
        patch_path = public.mt_jsonb_path_to_array((patch->>'path')::text, '\.');

        CASE patch->>'type'
            WHEN 'set' THEN
                retval = jsonb_set(retval, patch_path,(patch->'value')::jsonb, TRUE);
        WHEN 'delete' THEN
                retval = retval#-patch_path;
        WHEN 'append' THEN
                retval = public.mt_jsonb_append(retval, patch_path,(patch->'value')::jsonb, FALSE);
        WHEN 'append_if_not_exists' THEN
                retval = public.mt_jsonb_append(retval, patch_path,(patch->'value')::jsonb, TRUE);
        WHEN 'insert' THEN
                retval = public.mt_jsonb_insert(retval, patch_path,(patch->'value')::jsonb,(patch->>'index')::integer, FALSE);
        WHEN 'insert_if_not_exists' THEN
                retval = public.mt_jsonb_insert(retval, patch_path,(patch->'value')::jsonb,(patch->>'index')::integer, TRUE);
        WHEN 'remove' THEN
                retval = public.mt_jsonb_remove(retval, patch_path,(patch->'value')::jsonb);
        WHEN 'duplicate' THEN
                retval = public.mt_jsonb_duplicate(retval, patch_path,(patch->'targets')::jsonb);
        WHEN 'rename' THEN
                retval = public.mt_jsonb_move(retval, patch_path,(patch->>'to')::text);
        WHEN 'increment' THEN
                retval = public.mt_jsonb_increment(retval, patch_path,(patch->>'increment')::numeric);
        WHEN 'increment_float' THEN
                retval = public.mt_jsonb_increment(retval, patch_path,(patch->>'increment')::numeric);
        ELSE NULL;
        END CASE;
    END LOOP;
    RETURN retval;
END;
$function$;


DROP TABLE IF EXISTS public.mt_doc_weatherforecast2 CASCADE;
CREATE TABLE public.mt_doc_weatherforecast2 (
    id                  uuid                        NOT NULL,
    data                jsonb                       NOT NULL,
    mt_last_modified    timestamp with time zone    NULL DEFAULT (transaction_timestamp()),
    mt_version          uuid                        NOT NULL DEFAULT (md5(random()::text || clock_timestamp()::text)::uuid),
    mt_dotnet_type      varchar                     NULL,
CONSTRAINT pkey_mt_doc_weatherforecast2_id PRIMARY KEY (id)
);

CREATE OR REPLACE FUNCTION public.mt_upsert_weatherforecast2(doc JSONB, docDotNetType varchar, docId uuid, docVersion uuid) RETURNS UUID LANGUAGE plpgsql SECURITY INVOKER AS $function$
DECLARE
  final_version uuid;
BEGIN
INSERT INTO public.mt_doc_weatherforecast2 ("data", "mt_dotnet_type", "id", "mt_version", mt_last_modified) VALUES (doc, docDotNetType, docId, docVersion, transaction_timestamp())
  ON CONFLICT (id)
  DO UPDATE SET "data" = doc, "mt_dotnet_type" = docDotNetType, "mt_version" = docVersion, mt_last_modified = transaction_timestamp();

  SELECT mt_version FROM public.mt_doc_weatherforecast2 into final_version WHERE id = docId ;
  RETURN final_version;
END;
$function$;


CREATE OR REPLACE FUNCTION public.mt_insert_weatherforecast2(doc JSONB, docDotNetType varchar, docId uuid, docVersion uuid) RETURNS UUID LANGUAGE plpgsql SECURITY INVOKER AS $function$
BEGIN
INSERT INTO public.mt_doc_weatherforecast2 ("data", "mt_dotnet_type", "id", "mt_version", mt_last_modified) VALUES (doc, docDotNetType, docId, docVersion, transaction_timestamp());

  RETURN docVersion;
END;
$function$;


CREATE OR REPLACE FUNCTION public.mt_update_weatherforecast2(doc JSONB, docDotNetType varchar, docId uuid, docVersion uuid) RETURNS UUID LANGUAGE plpgsql SECURITY INVOKER AS $function$
DECLARE
  final_version uuid;
BEGIN
  UPDATE public.mt_doc_weatherforecast2 SET "data" = doc, "mt_dotnet_type" = docDotNetType, "mt_version" = docVersion, mt_last_modified = transaction_timestamp() where id = docId;

  SELECT mt_version FROM public.mt_doc_weatherforecast2 into final_version WHERE id = docId ;
  RETURN final_version;
END;
$function$;


     Npgsql.PostgresException: 25006: cannot execute CREATE FUNCTION in a read-only transaction                                                                                                                                                                                                                                                                                                                                                                             
       at async ValueTask<IBackendMessage> Npgsql.Internal.NpgsqlConnector.ReadMessageLong(bool async, DataRowLoadingMode dataRowLoadingMode, bool readingNotifications, bool isReadingPrependedMessage)                                                                                                                                                                                                                                                                    
       at TResult System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(short token)                                                                                                                                                                                                                                                                                    
       at async Task<bool> Npgsql.NpgsqlDataReader.NextResult(bool async, bool isConsuming, CancellationToken cancellationToken)                                                                                                                                                                                                                                                                                                                                            
       at async Task<bool> Npgsql.NpgsqlDataReader.NextResult(bool async, bool isConsuming, CancellationToken cancellationToken)                                                                                                                                                                                                                                                                                                                                            
       at async ValueTask<NpgsqlDataReader> Npgsql.NpgsqlCommand.ExecuteReader(bool async, CommandBehavior behavior, CancellationToken cancellationToken)                                                                                                                                                                                                                                                                                                                   
       at async ValueTask<NpgsqlDataReader> Npgsql.NpgsqlCommand.ExecuteReader(bool async, CommandBehavior behavior, CancellationToken cancellationToken)                                                                                                                                                                                                                                                                                                                   
       at async Task<int> Npgsql.NpgsqlCommand.ExecuteNonQuery(bool async, CancellationToken cancellationToken)                                                                                                                                                                                                                                                                                                                                                             
       at async Task Weasel.Postgresql.PostgresqlMigrator.executeDelta(SchemaMigration migration, DbConnection conn, AutoCreate autoCreate, IMigrationLogger logger, CancellationToken ct)                                                                                                                                                                                                                                                                                  
  at void Marten.StoreOptions.Weasel.Core.Migrations.IMigrationLogger.OnFailure(DbCommand command, Exception ex)                                                                                                                                                                                                                                                                                                                                                            
  at async Task Weasel.Postgresql.PostgresqlMigrator.executeDelta(SchemaMigration migration, DbConnection conn, AutoCreate autoCreate, IMigrationLogger logger, CancellationToken ct)                                                                                                                                                                                                                                                                                       
  at async Task<SchemaPatchDifference> Weasel.Core.Migrations.DatabaseBase`1.ApplyAllConfiguredChangesToDatabaseAsync(IGlobalLock<TConnection> globalLock, AutoCreate? override, ReconnectionOptions reconnectionOptions, CancellationToken ct)                                                                                                                                                                                                                             
  at async Task<SchemaPatchDifference> Weasel.Core.Migrations.DatabaseBase`1.ApplyAllConfiguredChangesToDatabaseAsync(IGlobalLock<TConnection> globalLock, AutoCreate? override, ReconnectionOptions reconnectionOptions, CancellationToken ct)                                                                                                                                                                                                                             
  at async Task Marten.Services.MartenActivator.StartAsync(CancellationToken cancellationToken)                                                                                                                                                                                                                                                                                                                                                                             
  at async Task Microsoft.Extensions.Hosting.Internal.Host.<StartAsync>b__15_1(IHostedService service, CancellationToken token)                                                                                                                                                                                                                                                                                                                                             
  at async Task Microsoft.Extensions.Hosting.Internal.Host.ForeachService<T>(IEnumerable<T> services, CancellationToken token, bool concurrent, bool abortOnFirstException, List<Exception> exceptions, Func<T, CancellationToken, Task> operation)                                                                                                                                                                                                                         
  at async Task Microsoft.Extensions.Hosting.Internal.Host.StartAsync(CancellationToken cancellationToken)                                                                                                                                                                                                                                                                                                                                                                  
  at async Task<bool> Oakton.Commands.RunCommand.Execute(RunInput input)                                                                                                                                                                                                                                                                                                                                                                                                    
  at async Task<int> Oakton.CommandExecutor.execute(CommandRun run)                                                                                                                                                                                                                                                                                                                                                                                                         


```