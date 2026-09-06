CASE
    WHEN btrim(coalesce(firstname, '')) = '' AND btrim(coalesce(lastname, '')) = ''
        THEN user_name
    ELSE coalesce(firstname, '') || ' ' || coalesce(lastname, '')
END
