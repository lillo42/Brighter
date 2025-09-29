ALTER SESSION SET CONTAINER=BRIGHTER_DATABASE;

-- 2. Then, grant the privilege on the correct package
GRANT EXECUTE ON SYS.DBMS_AQADM TO BRIGHTER;

-- 3. Then, grant the privilege on the correct package
GRANT AQ_ADMINISTRATOR_ROLE TO brighter;

-- 4. Finally, commit the transaction
COMMIT;
