WITH w AS (
                SELECT *
                FROM stock_watch_js01 swj
                WHERE stock_date = '20250924'
                -- AND ma120_grad >= 0
                AND ma60_grad >= 0
                --AND ma20_grad >= 0
                AND (ma5_ago1_prc < ma20_ago1_prc OR  ma5_ago2_prc < ma20_ago2_prc)
                AND avg_ac_trans_prc > 50 * 100
                ),
                c AS (
                        SELECT *
                        FROM kw_real_data krd
                        WHERE 1=1
                                        /*AND acc_trans_prc > 25 * 100*/
                                        AND fid_228 > 105


                        AND krd.reg_dt BETWEEN to_timestamp(CONCAT('20250924', '092622'),'YYYYMMDDHH24MISS') - ( 90 || 'second' ) ::INTERVAL
                        AND to_timestamp(CONCAT('20250924', '092622'),'YYYYMMDDHH24MISS')



                        AND stock_cd IN (
                                SELECT stock_cd
                                FROM stock_watch_js01 swj
                                WHERE stock_date = '20250924'
                                --AND ma120_grad >= 0
                                AND ma60_grad >= 0
                                --AND ma20_grad >= 0
                                AND (ma5_ago1_prc < ma20_ago1_prc OR  ma5_ago2_prc < ma20_ago2_prc)
                                AND avg_ac_trans_prc > 50 * 100
                        )
                )
                SELECT w.stock_cd
                FROM (
                        SELECT
                                w.stock_cd,
                                min(real_data_uid) AS real_data_uid
                        FROM w
                        JOIN c ON w.stock_cd = c.stock_cd
                        WHERE 1=1
                        AND (w.sum4_prc + c.curr_prc) / (w.sum4_cnt + 1) > (w.sum19_prc + c.curr_prc) / (w.sum19_cnt + 1)
                        GROUP BY w.stock_cd
                ) sig2
                JOIN c ON sig2.real_data_uid  = c.real_data_uid
                JOIN w ON sig2.stock_cd = w.stock_cd
I0924:092622:011 Log4JdbcCustomFormatter(0074) SQL : WITH w AS (
    SELECT m.hhmm,w.*



  FROM (
    SELECT m.stock_cd, min(hhmm) AS hhmm, min(m.day_start_prc) AS day_start_prc
    FROM stock_minute3_chart m
    WHERE m.stock_cd IN (
      (SELECT stock_cd
      FROM stock_minute3_chart smc
      WHERE reg_dt = to_timestamp(CONCAT('20250924', '080000'), 'YYYYMMDDHH24MISS') + (3 * 60 ||'seconds') ::INTERVAL
      AND fn_per(day_start_prc, day_base_prc) > 4
      --AND start_prc < close_prc
      GROUP BY stock_cd)
    )
    AND m.reg_dt > to_timestamp(CONCAT('20250924', '090000'), 'YYYYMMDDHH24MISS') + (3 * 60 ||'seconds') ::INTERVAL
    AND m.reg_dt <= to_timestamp(CONCAT('20250924', '092622'),'YYYYMMDDHH24MISS') + ( 210 || 'second' ) ::INTERVAL
    AND m.day_start_prc > m.start_prc
    GROUP BY m.stock_cd
  ) m
   JOIN stock_watch_js01 w ON (m.stock_cd = w.stock_cd)
   AND w.stock_date = '20250924'
   AND w.base_prc >= w.ma120_ago1_prc
   AND w.base_prc >= w.ma60_ago1_prc
   AND (w.high_ago1_prc + w.base_prc) /2 * 0.99 < m.day_start_prc
   AND (fn_per(w.base_prc, w.ma240_ago1_prc ) < -5 OR fn_per(w.base_prc, w.ma240_ago1_prc ) > 0)
   AND w.ma5_grad >=0
   AND fn_per(w.base_prc,w.day4_low_prc) < 30



  ) ,
  c AS (
    SELECT
    *
    FROM kw_real_data krd
    WHERE 1=1
    AND abs(curr_prc) - fid_11 > 1000
    AND fn_per(abs(start_prc), abs(curr_prc) - fid_11) > 4
    AND start_prc < curr_prc


      AND stock_cd IN  (  '377480' )


      AND krd.reg_dt > to_timestamp(CONCAT('20250924', '090600'), 'YYYYMMDDHH24MISS')
      AND krd.reg_dt BETWEEN to_timestamp(CONCAT('20250924', '092622'),'YYYYMMDDHH24MISS') - ( 90 || 'second' ) ::INTERVAL
      AND to_timestamp(CONCAT('20250924', '092622'),'YYYYMMDDHH24MISS')



  )
  SELECT
    *
  FROM (
    SELECT
      'HS22' AS formula_tp,
      fn_stock_nm(w.stock_cd) AS stock_nm,
      fn_per(m1.ma5_ago1_prc, m1.ma20_ago1_prc) AS per,
      fn_nv_url(w.stock_cd) AS url,
      fn_per(c.curr_prc, w.recent_high_prc) AS high_per,
      fn_per(c.high_prc, w.recent_high_prc) AS close_per,
      (SELECT count(*) FROM stock_day_chart WHERE stock_cd = w.stock_cd AND stock_date >= fn_days_ago('20250924',5) AND stock_date < '20250924' AND high_prc > c.curr_prc) AS cnt,
      '20250924' AS stock_date,
      3 AS duration,
      c.hhmmss AS rcmd_hhmm,
      c.curr_prc AS rcmd_prc,
      c.fid_12 as rcmd_per,
      fn_per(abs(c.start_prc) , (c.curr_prc - c.fid_11) ) AS start_per,



      *
    FROM (
      SELECT
        w.stock_cd,
        min(real_data_uid) AS real_data_uid
      FROM w
      JOIN c ON w.stock_cd = c.stock_cd
      WHERE LEFT(c.hhmmss, 4) >= w.hhmm
      -- AND w.recent_high_prc < c.curr_prc
      GROUP BY w.stock_cd
    ) sig2
    JOIN c ON sig2.real_data_uid = c.real_data_uid
    JOIN w ON sig2.stock_cd = w.stock_cd
    JOIN stock_watch_min1 m1 ON (m1.stock_date = '20250924' AND m1.stock_cd =c.stock_cd )
    WHERE left(c.hhmmss,4)  = m1.hhmm
    AND fn_per(m1.ma5_ago1_prc, m1.ma20_ago1_prc) < 3
    AND m1.ma5_grad >= 0

  ) a
 -- WHERE a.cnt = 0
  ORDER BY a.hhmmss ASC
