WITH day0 AS (
    SELECT
      stock_cd,
      stock_date AS day0_date,
      start_prc AS day0_open,
      curr_prc AS day0_close,
      high_prc AS day0_high,
      acc_stock_vol AS day0_vol,
      ROUND((curr_prc - start_prc)::numeric / NULLIF(start_prc,0) * 100, 2) AS day0_body_per,
      (high_prc - GREATEST(curr_prc, start_prc)) AS day0_upper_shadow,
      abs(curr_prc - start_prc) AS day0_body_size
    FROM stock_day_chart
    WHERE stock_date = fn_days_ago('20250924', 2)
  ),
  day1 AS (
    SELECT
      stock_cd,
      stock_date AS day1_date,
      start_prc AS day1_open,
      curr_prc AS day1_close,
      acc_stock_vol AS day1_vol,
      ROUND((curr_prc - start_prc)::numeric / NULLIF(start_prc,0) * 100, 2) AS day1_body_per
    FROM stock_day_chart
    WHERE stock_date = fn_days_ago('20250924', 1)
  ),
  w AS (
   SELECT
    *
   FROM stock_watch_js01 w
   WHERE stock_date ='20250924'
   AND w.base_prc < 200000
   AND w.base_prc > 1000
   AND ma5_grad > 0
  ),
  candidates AS (
    SELECT
      d0.stock_cd,
      d0.day0_date,
      d1.day1_date,
      d0.day0_open,
      d0.day0_close,
      d0.day0_high,
      d1.day1_close,
      d0.day0_body_per,
      d1.day1_body_per,
      d0.day0_vol,
      d1.day1_vol,
      d0.day0_upper_shadow,
      d0.day0_body_size
    FROM day0 d0
    JOIN day1 d1 ON d0.stock_cd = d1.stock_cd
    JOIN w ON d0.stock_cd = w.stock_cd
    WHERE
      -- Day0 조건: 장대양봉 + 몸통비율 ≥ 3%
      d0.day0_close > d0.day0_open
      AND d0.day0_body_per >= 3
      -- 위꼬리 길이가 몸통의 50% 이하
      AND d0.day0_upper_shadow::numeric / NULLIF(d0.day0_body_size,0) < 0.5
      -- Day1 조건: 종가가 Day0 종가 이하 + 몸통 작음 + 거래량 감소
      AND d1.day1_close <= d0.day0_close
      AND abs(d1.day1_body_per) < d0.day0_body_per
      AND d1.day1_vol < d0.day0_vol
  -- Day1이 과도한 갭상승 음봉이면 제외
   AND NOT (
    d1.day1_open > d0.day0_close * 1.02
    AND d1.day1_close < d1.day1_open
    )
  )


     ,
    today AS (
      SELECT
      *
      FROM kw_real_data krd
      WHERE 1=1
      AND fn_per(curr_prc, high_prc) > -3.0 /*적은거래대금으로 이미 돌파한 경우를 제거하기 위함.*/
      AND krd.acc_trans_prc > 15 * 100
      AND stock_cd IN(SELECT stock_cd FROM candidates)


      AND krd.reg_dt > to_timestamp(CONCAT('20250924', '080000'), 'YYYYMMDDHH24MISS')
      AND krd.reg_dt BETWEEN to_timestamp(CONCAT('20250924', '092622'),'YYYYMMDDHH24MISS') - ( 90 || 'second' ) ::INTERVAL
      AND to_timestamp(CONCAT('20250924', '092622'),'YYYYMMDDHH24MISS')



    )
    SELECT
      'AI03' AS formula_tp,
      fn_stock_nm(c.stock_cd) AS stock_nm,
      fn_per(m1.ma5_ago1_prc, m1.ma20_ago1_prc) AS per,
      fn_nv_url(c.stock_cd) AS url,
      w.stock_date AS stock_date,
      c.hhmmss AS rcmd_hhmm,
      c.curr_prc AS rcmd_prc,
      c.fid_12 as rcmd_per,
      w.stock_cd,
      w1.day0_high AS target_prc,
      *
    FROM (
      SELECT
          t.stock_cd,
          min(real_data_uid) AS real_data_uid
      FROM candidates w
      JOIN today t ON w.stock_cd = t.stock_cd
      WHERE 1=1
        AND t.curr_prc >= w.day0_high
      GROUP BY t.stock_cd
    ) t
    JOIN w ON w.stock_cd = t.stock_cd
    JOIN candidates w1 ON w1.stock_cd = t.stock_cd
    JOIN today c ON t.real_data_uid = c.real_data_uid
    JOIN stock_watch_min1 m1 ON (m1.stock_date = '20250924' AND m1.stock_cd =c.stock_cd )
    WHERE left(c.hhmmss,4)  = m1.hhmm
    AND fn_per(m1.ma5_ago1_prc, m1.ma20_ago1_prc) < 4
    AND m1.ma5_grad >= 0
    ORDER BY c.hhmmss ASC
