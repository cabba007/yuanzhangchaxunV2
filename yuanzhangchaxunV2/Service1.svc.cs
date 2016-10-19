using System;
using System.Collections.Generic;
using System.Data.OracleClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace yuanzhangchaxunV2
{
    public class Service1 : IService1
    {
        public ServiceResult GetData(string q)
        {
            ServiceResult serviceResult = new ServiceResult();
            OracleConnection myConn = new OracleConnection("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=10.10.4.3)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME = orcl)));User ID=system;Password=manager");
            OracleCommand myCmd = new OracleCommand(null, myConn);
            DateTime time;
            if (DateTime.TryParse(q + " 00:00:00", out time))
            {
                if ((time > DateTime.Now) || (time < DateTime.Now.AddYears(-2)))
                {
                    time = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd") + " 00:00:00");
                }
            }
            else
            {
                time = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd") + " 00:00:00");
            }
            OracleParameter[] d = {
                                    new OracleParameter("d0", OracleType.Char) { Value = time.ToString("yyyy-MM-dd") },
                                    new OracleParameter("d1", OracleType.Char) { Value = time.AddDays((double)(1 - time.Day)).ToString("yyyy-MM-dd") + " 00:00:00" },
                                    new OracleParameter("d2", OracleType.Char) { Value = time.AddDays((double)(1 - time.Day)).AddMonths(1).ToString("yyyy-MM-dd") + " 00:00:00" },
                                    new OracleParameter("d3", OracleType.Char) { Value = time.AddMonths(-1).ToString("yyyy-MM-dd") }
                                  };

            myConn.Open();
            myCmd.CommandText = "Select sysdate from dual";
            DateTime fuwuqishijian = DateTime.Parse(myCmd.ExecuteScalar().ToString());
            myCmd.CommandText = "select nvl(count(*), 0), sum(case when CLINIC_MASTER.CLINIC_TYPE not like '急诊%' then 1 else 0 end), sum(case when CLINIC_MASTER.CLINIC_TYPE like '急诊%' then 1 else 0 end) from CLINIC_master where returned_date is null and VISIT_DATE >= to_date(:d0||' 00:00:00','yyyy-mm-dd hh24:mi:ss') and VISIT_DATE <= to_date(:d0||' 23:59:59','yyyy-mm-dd hh24:mi:ss')";
            myCmd.Parameters.Add(d[0]);
            OracleDataReader myReader = myCmd.ExecuteReader();
            myReader.Read();
            serviceResult.zongrenshu = myReader[0].ToString();
            double zongrenshu = Convert.ToDouble(myReader[0]);
            serviceResult.menzhenrenshu = myReader[1].ToString();
            serviceResult.jizhenrenshu = myReader[2].ToString();
            myCmd.Parameters.Clear();
            myCmd.CommandText = "select count(*) from CLINIC_master where returned_date is null and VISIT_DATE >= to_date(:d1, 'yyyy-mm-dd hh24:mi:ss') and VISIT_DATE <= to_date(:d2, 'yyyy-mm-dd hh24:mi:ss')";
            myCmd.Parameters.Add(d[1]);
            myCmd.Parameters.Add(d[2]);
            double dangyuezongrenshu = Convert.ToDouble(myCmd.ExecuteScalar().ToString());
            serviceResult.zhandangyuebili = (100.0 * zongrenshu / dangyuezongrenshu).ToString("F2");
            myCmd.CommandText = "select * from (select to_char(VISIT_DATE , 'yyyy-mm-dd') from CLINIC_master where VISIT_DATE >= to_date(:d1, 'yyyy-mm-dd hh24:mi:ss') and VISIT_DATE <= to_date(:d2, 'yyyy-mm-dd hh24:mi:ss') group by to_char(VISIT_DATE , 'yyyy-mm-dd') order by count (*) desc) where rownum <= 1";
            serviceResult.dangyuerencizuidari = myCmd.ExecuteScalar().ToString();
            myCmd.Parameters.Clear();
            myCmd.CommandText = "select sum ( nvl ( a.costs , 0 ) ) ,sum ( nvl ( a.charges , 0 ) ) , sum ( decode ( c.PRIORITY_INDICATOR , 1 , a.costs , 0 ) ) ,sum ( decode ( c.PRIORITY_INDICATOR , 1 , a.charges , 0 ) ) ,sum ( decode ( a.item_CLASS , 'A' , nvl ( a.costs , 0 ) , 'B' , nvl ( a.costs , 0 ) , 0 ) ) FROM OUTP_BILL_ITEMS A , OUTP_RCPT_MASTER b , IDENTITY_DICT c where a.RCPT_NO   =b.RCPT_NO ( + ) and b.IDENTITY    =c.IDENTITY_NAME ( + ) and A.VISIT_DATE >= to_date (:d0 ||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss') and A.VISIT_DATE <= to_date (:d0 ||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss')";
            myCmd.Parameters.Add(d[0]);
            myReader = myCmd.ExecuteReader();
            myReader.Read();
            serviceResult.menjizhenyaofeibi = (100.0 * Convert.ToDouble(myReader[4]) / Convert.ToDouble(myReader[0])).ToString("F2");
            myCmd.CommandText = "select sum ( NVL ( CLINIC_charge , 0 ) ) from clinic_master where returned_date is null and VISIT_DATE >= to_date (:d0 ||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss') and VISIT_DATE <= to_date (:d0 ||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss')";
            serviceResult.guahaoshouru = myCmd.ExecuteScalar().ToString();
            if (time.Date == DateTime.Today)
            {
                myCmd.CommandText = "select sum(nvl(COSTS,0)) FROM OUTP_BILL_ITEMS where VISIT_DATE >= to_date (:d0 ||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss') and VISIT_DATE <= to_date (:d0 ||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss')";
                serviceResult.riqi = "今天截至" + fuwuqishijian.ToString("HH:mm:ss");
            }
            else
            {
                myCmd.CommandText = "select sum ( nvl ( total_costs , 0 ) ) FROM ST_OUTD_BILL_DAY where ST_DATE >=  to_date (:d0 ||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss') and ST_DATE <= to_date (:d0 ||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss')";
                serviceResult.riqi = time.ToString("yyyy-MM-dd");
            }
            serviceResult.menjizhenshouru = myCmd.ExecuteScalar().ToString();
            if (time.Date == DateTime.Today)
            {
                myCmd.CommandText = "select sum ( nvl ( costs , 0 ) ) from inp_bill_detail where  BILLING_DATE_TIME >=to_date(:d0 ||' 00:00:00', 'yyyy-mm-dd hh24:mi:ss') and BILLING_DATE_TIME <= to_date(:d0 ||' 23:59:59', 'yyyy-mm-dd hh24:mi:ss')";
            }
            else
            {
                myCmd.CommandText = "select sum ( nvl ( total_costs , 0 ) ) from medadm.st_ind_bill_day where ST_DATE >= to_date (:d0 ||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss') and ST_DATE <= to_date (:d0 ||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss')";
            }
            serviceResult.zhuyuanshouru = myCmd.ExecuteScalar().ToString();
            myCmd.CommandText = "select nvl ( count ( *) , 0 ) , sum ( decode ( PRIORITY_INDICATOR , 1 , 1 , 0 ) ) from v_pat_visit a , IDENTITY_DICT b where a.IDENTITY  =b.IDENTITY_NAME ( + ) and ADMISSION_DATE_TIME >= to_date (:d0 ||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss') and ADMISSION_DATE_TIME <= to_date (:d0 ||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss')";
            serviceResult.ruyuanrenshu = myCmd.ExecuteScalar().ToString();
            myCmd.CommandText = "select nvl (count (*) , 0 ) from v_bedpats_in_hospital where stat_date_time =to_date(:d0, 'yyyy-mm-dd') and ward_code is not null";
            serviceResult.zaikerenshu = myCmd.ExecuteScalar().ToString();
            myCmd.CommandText = "select nvl ( count ( *) , 0 ) , sum ( decode ( PRIORITY_INDICATOR , 1 , 1 , 0 ) ) from v_pat_visit a , IDENTITY_DICT b where a.IDENTITY  =b.IDENTITY_NAME ( + ) and DISCHARGE_DATE_TIME  >= to_date (:d0 ||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss') and DISCHARGE_DATE_TIME  <= to_date (:d0 ||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss')";
            serviceResult.chuyuanrenshu = myCmd.ExecuteScalar().ToString();
            myCmd.CommandText = "select nvl ( count ( *) , 0 ) from v_pat_visit a , pats_in_hospital b where a.patient_id =b.patient_id and a.visit_id=b.visit_id and CHARGE_TYPE   in ( select charge_type_name from charge_type_dict where is_insur ='1')";
            myCmd.Parameters.Clear();
            serviceResult.yibaorenshu = myCmd.ExecuteScalar().ToString();
            myCmd.Parameters.Add(d[0]);
            myCmd.CommandText = "select nvl ( count ( *) , 0 ) from v_pat_visit a , v_bedpats_in_hospital b where a.patient_id   =b.patient_id and a.visit_id =b.visit_id and b.stat_date_time = to_date(:d0,'yyyy-mm-dd ') and CHARGE_TYPE ='新农合'";
            serviceResult.xinnongherenshu = myCmd.ExecuteScalar().ToString();
            myCmd.CommandText = "select sum ( nvl ( a.costs , 0 ) ) ,sum ( nvl ( a.charges , 0 ) ) , sum ( decode ( c.PRIORITY_INDICATOR , 1 , a.costs , 0 ) ) , sum ( decode (c.PRIORITY_INDICATOR , 1 , a.charges , 0 ) ) , sum ( decode ( a.item_CLASS , 'A' , nvl ( a.costs , 0 ) , 'B' , nvl ( a.costs , 0 ) , 0 ) ) , sum ( decode ( a.item_CLASS , 'I' , nvl ( a.costs , 0 ) , 0 ) ) from inp_bill_detail a , PAT_VISIT B , IDENTITY_DICT c where A.PATIENT_ID     =B.PATIENT_ID (+ ) AND A.VISIT_ID =B.VISIT_ID ( + ) AND b.IDENTITY=c.IDENTITY_NAME ( + ) AND BILLING_DATE_TIME >= To_DATE(:d0||' 00:00:00','yyyy-mm-dd hh24:mi:ss') AND BILLING_DATE_TIME <= To_DATE(:d0||' 23:59:59','yyyy-mm-dd hh24:mi:ss')";
            myReader = myCmd.ExecuteReader();
            myReader.Read();
            serviceResult.yingshou = myReader[0].ToString();
            serviceResult.shishou = myReader[1].ToString();
            serviceResult.youxianyingshou = myReader[2].ToString();
            serviceResult.youxianshishou = myReader[3].ToString();
            serviceResult.zhongxiyaoshoufei = myReader[4].ToString();
            serviceResult.cailiaofei = myReader[5].ToString();
            serviceResult.zhuyuanyaofeibilv = (100.0 * Convert.ToDouble(myReader[4]) / Convert.ToDouble(myReader[1])).ToString("F2");
            serviceResult.cailiaofeibilv = (100.0 * Convert.ToDouble(myReader[5]) / Convert.ToDouble(myReader[1])).ToString("F2");
            myCmd.Parameters.Clear();
            myCmd.CommandText = "select nvl ( count ( *) , 0 ) from pats_in_hospital where dept_code is null";
            serviceResult.dairukerenshu = myCmd.ExecuteScalar().ToString();
            myCmd.CommandText = "select nvl (count(*) , 0) from bed_rec where ( BED_APPROVED_TYPE =0 )";
            serviceResult.zaibianchuangwei = myCmd.ExecuteScalar().ToString();
            myCmd.CommandText = "select sum ( nvl ( aa.hz_bed_count , 0 ) ) as hz_bed_count , sum ( nvl ( bb.used_bed_count , 0 ) ) as used_bed_count , sum ( decode ( nvl ( aa.hz_bed_count , 0 ) , 0 , - bb.used_bed_count , decode ( sign ( aa.hz_bed_count-nvl ( bb.used_bed_count , 0 ) ) , 1 , aa.hz_bed_count-nvl ( bb.used_bed_count , 0 ) , 0 ) ) ) as empty_bed_count , sum ( decode ( nvl ( aa.hz_bed_count , 0 ) , 0 , 0 , decode ( sign ( nvl ( bb.used_bed_count , 0 ) - aa.hz_bed_count ) , 1 , nvl ( bb.used_bed_count , 0 ) - aa.hz_bed_count , 0 ) ) ) as add_bed_count from ( select pp.bq as bq , pp.bqm as bqm , pp.zkm as zkm , pp.zk as zk , sum ( pp.hz_bed_count ) hz_bed_count from ( select a.ward_code as bq , c.dept_name as bqm , b.dept_name as zkm , b.DEPT_code as zk , sum ( decode ( a.BED_APPROVED_TYPE , 0 , 1 , 0 ) ) hz_bed_count from bed_rec a , inq_dept_dict b , dept_dict c where a.ward_code =c.dept_code and a.DEPT_code =b.DEPT_code group by a.ward_code , c.dept_name , b.dept_name , b.DEPT_code union select nvl ( a.ward_code , '待入科' ) bq , c.dept_name , b.dept_name as zkm , a.DEPT_code as zk , sum ( 0 ) hz_bed_count from v_bedpats_in_hospital a , inq_dept_dict b , dept_dict c where a.ward_code =c.dept_code and a.DEPT_code =b.DEPT_code and stat_date_time >= to_date(:d0 || ' 00:00:00','yyyy-mm-dd hh24:mi:ss') and stat_date_time <= to_date( :d0 || ' 23:59:59','yyyy-mm-dd hh24:mi:ss') and bed_no is not null Group By a.Ward_Code , c.dept_name , b.dept_name , a.dept_code ) pp group by pp.bq , pp.bqm , pp.zkm , pp.zk ) aa , ( select bq , zk , sum ( used_bed_count ) as used_bed_count from ( select nvl ( ward_code , '待入科' ) bq , dept_code zk , count ( *) as used_bed_count from v_bedpats_in_hospital where stat_date_time >= to_date(:d0 || ' 00:00:00','yyyy-mm-dd hh24:mi:ss') and stat_date_time <= to_date(:d0 || ' 23:59:59','yyyy-mm-dd hh24:mi:ss') Group By Ward_Code , dept_code ) nn group by bq , zk ) bb where aa.bq =bb.bq ( + ) and aa.zk =bb.zk ( + )";
            myCmd.Parameters.Add(d[0]);
            myReader = myCmd.ExecuteReader();
            myReader.Read();
            serviceResult.jiachuangshu = myReader[3].ToString();
            serviceResult.kongchuangshu = myReader[2].ToString();
            serviceResult.chuangweishiyonglv = (100.0 * Convert.ToDouble(myReader[1]) / Convert.ToDouble(myReader[0])).ToString("F2");
            myCmd.CommandText = "select sum ( a.amount ) from outp_bill_items a where a.visit_date >= To_DATE(:d0||' 00:00:00','yyyy-mm-dd hh24:mi:ss') and a.visit_date <= To_DATE(:d0||' 23:59:59','yyyy-mm-dd hh24:mi:ss') and a.item_class='F'";
            object tmp = myCmd.ExecuteScalar();
            serviceResult.menzhenshoushu = tmp == null ? "0" : tmp.ToString();
            if (time.Date == DateTime.Today)
            {
                myCmd.CommandText = "select count ( *) , nvl ( sum ( decode ( a.operation_scale , '特' , 1 , 0 ) ) , 0 ) , nvl ( sum ( decode ( a.operation_scale , '大' , 1 , 0 ) ) , 0 ) , nvl ( sum ( decode ( a.operation_scale , '中' , 1 , 0 ) ) , 0 ) , nvl ( sum ( decode ( nvl ( a.operation_scale , '小' ) , '小' , 1 , 0 ) ) , 0 ) , nvl ( sum ( decode ( nvl ( a.emergency_indicator , 0 ) , 1 , 1 , 0 ) ) , 0 ) , nvl ( sum ( decode ( nvl ( a.emergency_indicator , 0 ) , 0 , 1 , 0 ) ) , 0 ) from operation_master a , OPERATION_NAME b where a.PATIENT_ID =b.patient_id and a.VISIT_ID =b.visit_id and a.OPER_ID =b.oper_id and a.visit_id > 0 and a.start_date_time >= To_DATE(:d0||' 00:00:00','yyyy-mm-dd hh24:mi:ss') and a.start_date_time <= To_DATE(:d0||' 23:59:59','yyyy-mm-dd hh24:mi:ss')";
            }
            else
            {
                myCmd.CommandText = "select nvl ( sum ( nvl ( emer_num , 0 ) ) + sum ( nvl ( nomal_num , 0 ) ) , 0 ) , nvl ( sum ( nvl ( great_oper_num , 0 ) ) , 0 ) , nvl ( sum ( nvl ( major_oper_num , 0 ) ) , 0 ) , nvl ( sum ( nvl ( medium_oper_num , 0 ) ) , 0 ) , nvl ( sum ( nvl ( minor_oper_num , 0 ) ) , 0 ) , nvl ( sum ( nvl ( emer_num , 0 ) ) , 0 ) , nvl ( sum ( nvl ( nomal_num , 0 ) ) , 0 ) from MEDADM.ST_DEPT_OPERATION_DAY where outp_or_inp ='1' and st_date >= To_DATE(:d0||' 00:00:00','yyyy-mm-dd hh24:mi:ss') and st_date <= To_DATE(:d0||' 23:59:59','yyyy-mm-dd hh24:mi:ss')";
            }
            myReader = myCmd.ExecuteReader();
            myReader.Read();
            serviceResult.zhuyuanshoushu = myReader[0].ToString();
            serviceResult.zhuyuanshoushu4 = myReader[1].ToString();
            serviceResult.zhuyuanshoushu3 = myReader[2].ToString();
            serviceResult.zhuyuanshoushu2 = myReader[3].ToString();
            serviceResult.zhuyuanshoushu1 = myReader[4].ToString();
            serviceResult.zhuyuanshoushujinji = myReader[5].ToString();
            serviceResult.zhuyuanshoushuzeqi = myReader[6].ToString();
            myCmd.CommandText = "select max ( a.md ) from (select to_char ( start_date_time , 'yyyy-mm-dd' ) as md , count ( *) as cnt from operation_master a , OPERATION_NAME b where a.PATIENT_ID =b.patient_id and a.VISIT_ID =b.visit_id and a.OPER_ID =b.oper_id and ( a.visit_id =0 or a.visit_id is null ) and a.start_date_time >= to_date ( SUBSTR ( :d0 , 1 , 7 ) || '-01 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' ) and a.start_date_time <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' ) group by to_char ( a.start_date_time , 'yyyy-mm-dd' ) ) a , (select max ( b.cnt ) max_ss from (select to_char ( a.start_date_time , 'yyyy-mm-dd' ) as md , count ( *) as cnt from operation_master a , OPERATION_NAME b where a.PATIENT_ID =b.patient_id and a.VISIT_ID =b.visit_id and a.OPER_ID =b.oper_id and ( a.visit_id =0 or a.visit_id is null ) and a.start_date_time >= to_date ( SUBSTR ( :d0 , 1 , 7 ) || '-01 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' ) and a.start_date_time <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' ) group by to_char ( a.start_date_time , 'yyyy-mm-dd' ) ) b ) c where a.cnt =c.max_ss";
            tmp = myCmd.ExecuteScalar();
            serviceResult.menzhenshoushudangyuezuidari = tmp == null ? "0" : tmp.ToString();
            myCmd.CommandText = "select max ( a.md ) from (select to_char ( start_date_time , 'yyyy-mm-dd' ) as md , count ( *) as cnt from operation_master a , OPERATION_NAME b where a.PATIENT_ID =b.patient_id and a.VISIT_ID =b.visit_id and a.OPER_ID =b.oper_id and a.visit_id >0 and a.start_date_time >= to_date ( SUBSTR ( :d0 , 1 , 7 ) || '-01 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' ) and a.start_date_time <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' ) group by to_char ( a.start_date_time , 'yyyy-mm-dd' ) ) a , (select max ( b.cnt ) max_ss from (select to_char ( a.start_date_time , 'yyyy-mm-dd' ) as md , count ( *) as cnt from operation_master a , OPERATION_NAME b where a.PATIENT_ID =b.patient_id and a.VISIT_ID =b.visit_id and a.OPER_ID =b.oper_id and a.visit_id >0 and a.start_date_time >= to_date ( SUBSTR ( :d0 , 1 , 7 ) || '-01 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' ) and a.start_date_time <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' ) group by to_char ( a.start_date_time , 'yyyy-mm-dd' ) ) b ) c where a.cnt =c.max_ss";
            tmp = myCmd.ExecuteScalar();
            serviceResult.zhuyuanshoushudangyuezuidari = tmp == null ? "0" : tmp.ToString();
            myCmd.CommandText = "select max ( a.md ) from (select to_char ( start_date_time , 'yyyy-mm-dd' ) as md , count ( *) as cnt from operation_master a , OPERATION_NAME b where a.PATIENT_ID =b.patient_id and a.VISIT_ID =b.visit_id and a.OPER_ID =b.oper_id and a.start_date_time >= to_date ( SUBSTR ( :d0 , 1 , 7 ) || '-01 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' ) and a.start_date_time <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' ) group by to_char ( a.start_date_time , 'yyyy-mm-dd' ) ) a , (select max ( b.cnt ) max_ss from (select to_char ( a.start_date_time , 'yyyy-mm-dd' ) as md , count ( *) as cnt from operation_master a , OPERATION_NAME b where a.PATIENT_ID =b.patient_id and a.VISIT_ID =b.visit_id and a.OPER_ID =b.oper_id and a.start_date_time >= to_date ( SUBSTR ( :d0 , 1 , 7 ) || '-01 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' ) and a.start_date_time <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' ) group by to_char ( a.start_date_time , 'yyyy-mm-dd' ) ) b ) c where a.cnt =c.max_ss";
            tmp = myCmd.ExecuteScalar();
            serviceResult.hejishoushudangyuezuidari = tmp == null ? "0" : tmp.ToString();
            if (time.Date == DateTime.Today)
            {
                myCmd.CommandText = "select COUNT ( a.test_no ) , sum ( nvl ( a.costs , 0 )) from LAB_TEST_MASTER a where a.RESULTS_RPT_DATE_TIME >= to_date ( :d0||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' ) and a.RESULTS_RPT_DATE_TIME <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' )";
            }
            else
            {
                myCmd.CommandText = "select sum(nvl(COMPLETED_NUM,0)), sum(nvl(TOTAL_COSTS,0)) from MEDADM.ST_DEPT_TEST_DAY_NEW where st_date >= to_date ( :d0||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' ) and st_date <= to_date( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' )";
            }
            myReader = myCmd.ExecuteReader();
            myReader.Read();
            serviceResult.huayanshu = myReader[0].ToString();
            serviceResult.huayanfeiyong = myReader[1].ToString();
            myCmd.CommandText = "select nvl ( count ( *) , 0 ) from CLINIC_master a , COMM.IDENTITY_DICT b where a.IDENTITY =b.IDENTITY_NAME and a.returned_date is null and b.PRIORITY_INDICATOR =0 and VISIT_DATE >= to_date ( :d0||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' ) and VISIT_DATE <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' )";
            serviceResult.dangtianputongjiuzhenrenshu = myCmd.ExecuteScalar().ToString();
            myCmd.CommandText = "select nvl ( count ( *) , 0 ) from CLINIC_master a , COMM.IDENTITY_DICT b where a.IDENTITY =b.IDENTITY_NAME and a.returned_date is null and b.PRIORITY_INDICATOR =1 and VISIT_DATE >= to_date ( :d0||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' ) and VISIT_DATE <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' )";
            serviceResult.dangtianyouxianjiuzhenrenshu = myCmd.ExecuteScalar().ToString();
            myCmd.CommandText = "select nvl(count(*),0) from pats_in_hospital where ADMISSION_DATE_TIME <= to_date(:d3||' 23:59:59','yyyy-mm-dd hh24:mi:ss') and ward_code is not null";
            myCmd.Parameters.Clear();
            myCmd.Parameters.Add(d[3]);
            serviceResult.zaike30tianyishangrenshu = myCmd.ExecuteScalar().ToString();
            myCmd.Parameters.Clear();
            myCmd.Parameters.Add(d[0]);
            myCmd.CommandText = "select sum ( decode ( exam_class , 'ＣＴ' , 1 , 'CT' , 1 , 0 ) ) ct , sum ( decode ( exam_class , 'ＣＴ' , costs , 'CT' , costs , 0 ) ) ct_costs , sum ( decode ( exam_class , 'TCD' , 1 , 0 ) ) pgj , sum ( decode ( exam_class , 'TCD' , costs , 0 ) ) pgj_costs , sum ( decode ( exam_class , '病理' , 1 , 0 ) ) bl , sum ( decode ( exam_class , '病理' , costs , 0 ) ) bl_costs , sum ( decode ( exam_sub_class , '电子肠镜' , 1 , 0 ) ) cj , sum ( decode ( exam_sub_class , '电子肠镜' , costs , 0 ) ) cj_costs , sum ( decode ( exam_class , '超声' , 1 , 'B超' , 1 , 0 ) ) cs , sum ( decode ( exam_class , '超声' , costs , 'B超' , costs , 0 ) ) cs_costs , sum ( decode ( exam_class , '磁共振' , 1 , 'MRI' , 1 , 0 ) ) cgz , sum ( decode ( exam_class , '磁共振' , costs , 'MRI' , costs , 0 ) ) cgz_costs , sum ( decode ( exam_class , '放射' , 1 , 0 ) ) fs , sum ( decode ( exam_class , '放射' , costs , 0 ) ) fs_costs , sum ( decode ( exam_class , '肺功能室' , 1 , 0 ) ) fgn , sum ( decode ( exam_class , '肺功能室' , costs , 0 ) ) fgn_costs , sum ( decode ( exam_class , '纤支镜室' , 1 , 0 ) ) hyx , sum ( decode ( exam_class , '纤支镜室' , costs , 0 ) ) hyx_costs , sum ( decode ( exam_class , '血管室' , 1 , 0 ) ) mnx , sum ( decode ( exam_class , '血管室' , costs , 0 ) ) mnx_costs , sum ( decode ( exam_class , '脑电图' , 1 , 0 ) ) ndt , sum ( decode ( exam_class , '脑电图' , costs , 0 ) ) ndt_costs , sum ( decode ( exam_sub_class , '电子胃镜' , 1 , 0 ) ) wj , sum ( decode ( exam_sub_class , '电子胃镜' , costs , 0 ) ) wj_costs , sum ( decode ( exam_class , '心电图' , 1 , 0 ) ) xdt , sum ( decode ( exam_class , '心电图' , costs , 0 ) ) xdt_costs from exam_master where scheduled_date_time >= to_date ( :d0||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' ) and scheduled_date_time <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' )";
            myReader = myCmd.ExecuteReader();
            myReader.Read();
            serviceResult.ctshu = myReader[0].ToString();
            serviceResult.ctfeiyong = myReader[1].ToString();
            serviceResult.tcdshu = myReader[2].ToString();
            serviceResult.tcdfeiyong = myReader[3].ToString();
            serviceResult.binglishu = myReader[4].ToString();
            serviceResult.binglifeiyong = myReader[5].ToString();
            serviceResult.changjingshu = myReader[6].ToString();
            serviceResult.changjingfeiyong = myReader[7].ToString();
            serviceResult.chaoshengshu = myReader[8].ToString();
            serviceResult.chaoshengfeiyong = myReader[9].ToString();
            serviceResult.cigongzhenshu = myReader[10].ToString();
            serviceResult.cigongzhenfeiyong = myReader[11].ToString();
            serviceResult.fangsheshu = myReader[12].ToString();
            serviceResult.fangshefeiyong = myReader[13].ToString();
            serviceResult.feigongnengshu = myReader[14].ToString();
            serviceResult.feigongnengfeiyong = myReader[15].ToString();
            serviceResult.xianzhijingshu = myReader[16].ToString();
            serviceResult.xianzhijingfeiyong = myReader[17].ToString();
            serviceResult.xueguanshishu = myReader[18].ToString();
            serviceResult.xueguanshifeiyong = myReader[19].ToString();
            serviceResult.naodiantushu = myReader[20].ToString();
            serviceResult.naodiantufeiyong = myReader[21].ToString();
            serviceResult.weijingshu = myReader[22].ToString();
            serviceResult.weijingfeiyong = myReader[23].ToString();
            serviceResult.xindiantushu = myReader[24].ToString();
            serviceResult.xindiantufeiyong = myReader[25].ToString();
            myCmd.CommandText = "select sum ( decode ( PATIENT_CONDITION , '1' , 1 , 0 ) ) , sum ( decode ( PATIENT_CONDITION , '2' , 1 , 0 ) ) from v_bedpats_in_hospital where stat_date_time = to_date(:d0,'yyyy-mm-dd ')";
            myReader = myCmd.ExecuteReader();
            myReader.Read();
            serviceResult.bingweirenshu = myReader[0].ToString();
            serviceResult.bingzhongrenshu = myReader[1].ToString();
            myCmd.CommandText = "select count(distinct(pe_visit.pe_id)), sum(pe_settle_bill_detail.charges) from pe_visit, pe_settle_bill_detail where (pe_visit.audit_date >= to_date ( :d0||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' ) and pe_visit.audit_date <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' )) and pe_visit.pe_id = pe_settle_bill_detail.pe_id and pe_visit.pe_visit_id = pe_settle_bill_detail.pe_visit_id";
            myReader = myCmd.ExecuteReader();
            myReader.Read();
            serviceResult.tijianrenshu = myReader[0].ToString();
            serviceResult.tijianfeiyong = myReader[1].ToString();
            myCmd.CommandText = "select nvl ( sum ( amount ) , 0.00 ) from inp_bill_detail where inp_bill_detail.item_code in ('05240100006B','05240300004','05240300005','05240300006','05240300009') and billing_date_TIME >= to_date ( :d0||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' ) and billing_date_TIME <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' )";
            serviceResult.fangliaoyeshu = myCmd.ExecuteScalar().ToString();
            myCmd.CommandText = @"
                                            select nvl ( sum ( amount ) , 0.00 )  , nvl ( sum ( costs ) , 0.00 )  from OUTP_BILL_ITEMS where item_name in ('神经传导速度测定','肌电图') and
                                            visit_date >= to_date ( :d0||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' )
                                            and visit_date <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' )";
            myReader = myCmd.ExecuteReader();
            myReader.Read();
            double aaa = Convert.ToDouble(myReader[0]);
            double bbb = Convert.ToDouble(myReader[1]);
            myCmd.CommandText = @"
                                            select nvl ( sum ( amount ) , 0.00 ) , nvl ( sum ( costs ) , 0.00 )  from inp_bill_detail where item_name in ('神经传导速度测定','肌电图') and
                                            billing_date_TIME >= to_date ( :d0||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' )
                                            and billing_date_TIME   <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' )";
            myReader = myCmd.ExecuteReader();
            myReader.Read();
            serviceResult.jidiantushu = (Convert.ToDouble(myReader[0]) + aaa).ToString();
            serviceResult.jidiantufeiyong = (Convert.ToDouble(myReader[1]) + bbb).ToString();
            myCmd.CommandText = @"
                                            select count ( distinct patient_id ) , 
                                            nvl ( sum ( costs ) , 0.00 ) 
                                            from inp_bill_detail 
                                            where performed_by ='0318' and 
                                            billing_date_TIME >= to_date ( :d0||' 00:00:00' , 'yyyy-mm-dd hh24:mi:ss' )
                                            and billing_date_TIME   <= to_date ( :d0||' 23:59:59' , 'yyyy-mm-dd hh24:mi:ss' )";
            myReader = myCmd.ExecuteReader();
            myReader.Read();
            serviceResult.fangliaorenshu = myReader[0].ToString();
            serviceResult.fangliaofeiyong = myReader[1].ToString();
            myConn.Close();
            return serviceResult;
        }

        public string UploadFile(string fileName, Stream stream)
        {
            using (FileStream writer = new FileStream(Path.Combine("\\\\10.10.33.173\\8288\\", fileName), FileMode.OpenOrCreate))
            {
                int readCount;
                var buffer = new byte[8192];
                while ((readCount = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    writer.Write(buffer, 0, readCount);
                }
            }
            return "success";
        }
    }
}
