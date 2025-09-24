 WITH sf AS (
                SELECT stock_cd AS sf_stock_cd
                FROM stock_fvrt
                WHERE user_uid = 1

                        AND stock_cd IN  (  '084990' )

                LIMIT 600 OFFSET 600 * 1
                )
                SELECT sc.stock_nm, a.*


                        FROM


                  (
                        SELECT
                            '092638'                                                                                                                      AS search_hhmmss,
                                '20250924'                                                                                                                       AS stock_date,
                                to_char(reg_dt, 'HH24MI')                                                                                                    AS hhmm,
                                max(hhmmss)                                                                                                                  AS hhmmss,
                                trunc(date_part('second', reg_dt) /60)                                                                                       AS sec_num,
                                stock_cd                                                                                                                     AS stock_cd,
                                LAG(stock_cd,1)                    OVER(ORDER BY stock_cd,to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60)) AS lag_stock_cd,
                                LAG(stock_cd,2)                    OVER(ORDER BY stock_cd,to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60)) AS lag2_stock_cd,
                                SUM(case when fid_15 > 0 then fid_15 else 0 end )                                                                            AS buy_amt,
                                LAG(SUM(case when fid_15 > 0 then fid_15 else 0 end ) * 100 / SUM(abs(fid_15) ),1) OVER(ORDER BY stock_cd,to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60))      AS lag_buy_amt_per,
                                SUM(case when fid_15 > 0 then fid_15 else 0 end ) * 100 / SUM(abs(fid_15) )                                                  AS buy_amt_per,
                                MAX(curr_prc - fid_11)                                                                                                       AS base_prc,
                                MAX(ABS(start_prc))                                                                                                          AS start_prc,
                                MAX((ABS(start_prc) - (ABS(curr_prc) - fid_11)) *100 /(ABS(curr_prc) - fid_11) ::float)                                      AS start_per,
                                MAX(curr_prc)                                                                                                                AS curr_prc,
                                LAG(MAX(curr_prc))                 OVER(ORDER BY stock_cd,to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60)) AS lag_curr_prc,
                                MAX(fid_12)                                                                                                                  AS fid12,
                                MAX(fn_per(ABS(high_prc), (curr_prc -fid_11)))                                        AS high_fid12,
                                MAX(fid_12) - LAG(MAX(fid_12))     OVER(ORDER BY stock_cd,to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60)) AS diff_fid12,
                                MAX(fid_12) - LAG(MAX(fid_12),2)   OVER(ORDER BY stock_cd,to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60)) AS diff_fid12_s2,
                                MAX(fid_228)                                                                                                                 AS curr_fid228,
                                LAG(MAX(fid_228))                  OVER(ORDER BY stock_cd,to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60)) AS lag_fid228,
                                SUM(SUM(fid_15))                   OVER(order by stock_cd,to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60)
                                                                                                           rows between unbounded preceding
 and current row) AS acc_fid15,
                                COUNT(*)                                                                                                                     AS all_cnt,
                                SUM(case when fid_15 > 0 then 1 else 0 end )                                                                                 AS buy_cnt,

                                SUM(abs(fid_15) )                                                                                                            AS all_amt,
                                MAX(fid_228) - LAG(MAX(fid_228))   OVER(ORDER BY stock_cd,to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60)) AS diff_fid228,
                                MAX(fid_228) - LAG(MAX(fid_228),2) OVER(ORDER BY stock_cd,to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60)) AS diff_fid228_s2,
                                SUM(case when fid_15 < 0 then 1 else 0 end )                                                                                 AS sell_cnt,
                                MIN(fid_29)                                                                                                                  AS fid29,
                                MAX(fid_30)                                                                                                                  AS fid30,
                                MAX(acc_stock_vol)                                                                                                           AS acc_stock_vol,
                                MAX(acc_trans_prc)                                                                                                           AS acc_trans_prc,
                                SUM(case when fid_15 > 0 then 1 else 0 end ) *100 / COUNT(*)                                                                 AS buy_per,
                                MAX(fn_fid228_per(fid_228))                                                                                                                                 AS curr_fid228_per,
                                LAG(max(fn_fid228_per(fid_228))) OVER(ORDER BY stock_cd,to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60))                                  AS lag_fid228_per,
                                MAX(fn_fid228_per(fid_228)) - LAG(max(fn_fid228_per(fid_228)))    OVER(ORDER BY stock_cd,to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60)) AS diff_fid228_per,
                                MAX(fn_fid228_per(fid_228)) - LAG(max(fn_fid228_per(fid_228)),2)  OVER(ORDER BY stock_cd,to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60)) AS diff_fid228_s2_per,




                SUM(case when fid_15 * curr_prc > 14000000 then 1 else 0 end ) as b14,
                                SUM(case when fid_15 * curr_prc > 15000000 then 1 else 0 end ) as b15,
                                SUM(case when fid_15 * curr_prc > 16000000 then 1 else 0 end ) as b16,
                                SUM(case when fid_15 * curr_prc > 17000000 then 1 else 0 end ) as b17,
                                SUM(case when fid_15 * curr_prc > 18000000 then 1 else 0 end ) as b18,
                                SUM(case when fid_15 * curr_prc > 19000000 then 1 else 0 end ) as b19,
                                SUM(case when fid_15 * curr_prc > 20000000 then 1 else 0 end ) as b20,
                                SUM(case when fid_15 * curr_prc > 21000000 then 1 else 0 end ) as b21,
                                SUM(case when fid_15 * curr_prc > 22000000 then 1 else 0 end ) as b22,
                                SUM(case when fid_15 * curr_prc > 23000000 then 1 else 0 end ) as b23,
                                SUM(case when fid_15 * curr_prc > 24000000 then 1 else 0 end ) as b24,
                                SUM(case when fid_15 * curr_prc > 25000000 then 1 else 0 end ) as b25,
                                SUM(case when fid_15 * curr_prc > 26000000 then 1 else 0 end ) as b26,
                                SUM(case when fid_15 * curr_prc > 27000000 then 1 else 0 end ) as b27,
                                SUM(case when fid_15 * curr_prc > 28000000 then 1 else 0 end ) as b28,
                                SUM(case when fid_15 * curr_prc > 29000000 then 1 else 0 end ) as b29,
                                SUM(case when fid_15 * curr_prc > 30000000 then 1 else 0 end ) as b30,
                                SUM(case when fid_15 * curr_prc > 31000000 then 1 else 0 end ) as b31,
                                SUM(case when fid_15 * curr_prc > 32000000 then 1 else 0 end ) as b32,
                                SUM(case when fid_15 * curr_prc > 33000000 then 1 else 0 end ) as b33,
                                SUM(case when fid_15 * curr_prc > 34000000 then 1 else 0 end ) as b34,
                                SUM(case when fid_15 * curr_prc > 35000000 then 1 else 0 end ) as b35,
                                SUM(case when fid_15 * curr_prc > 36000000 then 1 else 0 end ) as b36,
                                SUM(case when fid_15 * curr_prc > 37000000 then 1 else 0 end ) as b37,
                                SUM(case when fid_15 * curr_prc > 38000000 then 1 else 0 end ) as b38,
                                SUM(case when fid_15 * curr_prc > 39000000 then 1 else 0 end ) as b39,
                                SUM(case when fid_15 * curr_prc > 40000000 then 1 else 0 end ) as b40,
                                SUM(case when fid_15 * curr_prc > 41000000 then 1 else 0 end ) as b41,
                                SUM(case when fid_15 * curr_prc > 42000000 then 1 else 0 end ) as b42,
                                SUM(case when fid_15 * curr_prc > 43000000 then 1 else 0 end ) as b43,
                                SUM(case when fid_15 * curr_prc > 44000000 then 1 else 0 end ) as b44,
                                SUM(case when fid_15 * curr_prc > 45000000 then 1 else 0 end ) as b45,
                                SUM(case when fid_15 * curr_prc > 46000000 then 1 else 0 end ) as b46,
                                SUM(case when fid_15 * curr_prc > 47000000 then 1 else 0 end ) as b47,
                                SUM(case when fid_15 * curr_prc > 48000000 then 1 else 0 end ) as b48,
                                SUM(case when fid_15 * curr_prc > 49000000 then 1 else 0 end ) as b49,

                                SUM(case when fid_15 * curr_prc < -14000000 then 1 else 0 end ) as m14,
                                SUM(case when fid_15 * curr_prc < -15000000 then 1 else 0 end ) as m15,
                                SUM(case when fid_15 * curr_prc < -16000000 then 1 else 0 end ) as m16,
                                SUM(case when fid_15 * curr_prc < -17000000 then 1 else 0 end ) as m17,
                                SUM(case when fid_15 * curr_prc < -18000000 then 1 else 0 end ) as m18,
                                SUM(case when fid_15 * curr_prc < -19000000 then 1 else 0 end ) as m19,
                                SUM(case when fid_15 * curr_prc < -20000000 then 1 else 0 end ) as m20,



                                SUM(case when fid_15 * curr_prc > 20000000        then 1  else 0 end )                                                AS big_amount_cnt,
                                SUM(case when fid_15 * curr_prc < (-1 * 20000000) then 1  else 0 end )                                                AS minus_big_amount_cnt


                        FROM(
                        SELECT
                        reg_dt + (  '092638'::int % 10 || 'second' ) ::INTERVAL AS reg_dt,
                        reg_dt AS o_reg_dt,
                        hhmmss,
                        stock_cd,
                        fid_228,
                        curr_prc,
                        fid_11,
                        start_prc,
                        fid_12,
                        high_prc,
                        fid_29,
                        fid_30,
                        acc_stock_vol,
                        acc_trans_prc,
                        fid_15
                        FROM kw_real_data krd
                        INNER JOIN sf ON krd.stock_cd  = sf.sf_stock_cd
                        WHERE  1 = 1
                        AND reg_dt BETWEEN To_timestamp(concat('20250924','092638'), 'YYYYMMDDHH24MISS') - ( 60*5 || 'second' ) ::interval
                        AND                To_timestamp(concat('20250924','092638'), 'YYYYMMDDHH24MISS') + (  0 || 'second' ) ::interval

                                AND stock_cd IN  (  '084990' )

                        ) a
                        GROUP  BY stock_cd, to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60)
                        ORDER  BY stock_cd, to_char(reg_dt, 'HH24MI'), trunc(date_part('second', reg_dt) /60)
                        ) a


                LEFT JOIN stock_code sc on a.stock_cd = sc.stock_cd

                WHERE a.stock_cd = lag_stock_cd
                        AND a.stock_cd = lag2_stock_cd
                        AND big_amount_cnt >= 5
                        AND (big_amount_cnt *100 /(big_amount_cnt + minus_big_amount_cnt)) >= 75
                        AND (buy_cnt * 100 / all_cnt)  > 70.0
                        AND buy_cnt >= 100
                        AND buy_amt_per >= 80.0
                        AND curr_fid228 < 500
                        AND lag_fid228 < 500
                    AND high_fid12 - fid12 < 3.5
                    AND start_per < 3.5
