using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using SimulacionBot.Models;
using SimulacionBot.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace SimulacionBot.Controllers
{
    public class SimulationController : Controller
    {
        private DBSimulacionBotEntities db = new DBSimulacionBotEntities();

        // GET: Simulation
        public ActionResult Index()
        {
            return View(db.DT_Mensajes_Telefonica_DigitalBlue_Bk.Where(x => x.Estado == false).ToList());
        }
        // GET: Borrar Datos
        public ActionResult Delete(int tipo)
        {
            Session["Tipo"] = tipo;
            if (tipo == 1)
            {
                Session["TipoNombre"] = "Domicilios";
            }
            else if (tipo == 2)
            {
                Session["TipoNombre"] = "Rechazos";
            }
            else if (tipo == 3)
            {
                Session["TipoNombre"] = "Pendientes";
            }
            
            return View(db.DT_Mensajes_Telefonica_DigitalBlue_Bk.Where(x => x.Estado==false && x.TipoBase==tipo).ToList());
        }
        
        [HttpPost]
        public ActionResult DeleteR()
        {
            var activos = db.DT_Mensajes_Telefonica_DigitalBlue_Bk.Where(x => x.Estado == false);
            db.DT_Mensajes_Telefonica_DigitalBlue_Bk.RemoveRange(activos);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult DeleteAll()
        {
            int tipo = (int)Session["Tipo"];
            var activos = db.DT_Mensajes_Telefonica_DigitalBlue_Bk.Where(x => x.Estado == false && x.TipoBase==tipo);
            var vacio = activos.Count();
            if (vacio>0)
            {
                db.DT_Mensajes_Telefonica_DigitalBlue_Bk.RemoveRange(activos);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public void Descargar()
        {
            int tipo = (int)Session["Tipo"];
            var lista = db.DT_Mensajes_Telefonica_DigitalBlue_Bk.Where(x=> x.Estado==false && x.TipoBase==tipo).ToList();
            var listaDT = ConvertListToDatatable.ToDataTable(lista);
            //listaDT.Columns.Remove("__v");
            listaDT.Columns.Remove("Id");
            listaDT.Columns.Remove("CreatedAt");
            listaDT.Columns.Remove("Estado");
            listaDT.Columns.Remove("UpdateAt");
            listaDT.Columns.Remove("EstadoCliente");
            listaDT.Columns.Remove("TipoBase");
            //listaDT.Columns.Remove("Id");
            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("content-disposition", "attachment;filename=" + HttpUtility.UrlEncode("ReportesBot_"+ DateTime.UtcNow.ToString("MM-dd-yyyy") + ".xlsx", System.Text.Encoding.UTF8));

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            //ExcelPackage.LicenseContext = System.ComponentModel.LicenseContext.NonCommercial;
            using (ExcelPackage pck = new ExcelPackage())
            {
                ExcelWorksheet ws = pck.Workbook.Worksheets.Add("Clientes");

                ws.Cells["A1"].LoadFromDataTable(listaDT, true);
                ws.Cells["A1"].Value = "Telefono";
                ws.Cells["B1"].Value = "Mensaje";
                ws.Cells["C1"].Value = "Motivo";

                var ms = new System.IO.MemoryStream();
                pck.SaveAs(ms);
                ms.WriteTo(Response.OutputStream);
            }

            Response.Flush();
            Response.End();
        }
        public ActionResult Import (HttpPostedFileBase file)
        {
            var list = new List<DT_Mensajes_Telefonica_DigitalBlue_Bk>();
            if (file == null || !file.FileName.Contains("xlsx"))
            {
                ViewData["Mensaje"] = "Archivo no encontrado o no seleccionado intenta de nuevo";
                db.SaveChanges();
                return View("Index", db.DT_Mensajes_Telefonica_DigitalBlue_Bk.Where(x => x.Estado == false).ToList());
            }
            using (var package = new ExcelPackage(file.InputStream))
            {

                // get the first worksheet in the workbook
                int tipoBase = 1;
                if (file.FileName.Contains("PENDIENTES"))
                {
                    tipoBase = 2;
                }
                if (file.FileName.Contains("RECHAZOS"))
                {
                    tipoBase = 3;
                }
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int col = 1;

                for (int row = 2; worksheet.Cells[row, col].Value != null; row++)
                {
                    // do something with worksheet.Cells[row, col].Value
                    list.Add(new DT_Mensajes_Telefonica_DigitalBlue_Bk
                    {
                        Telefono = worksheet.Cells[row, 1].Value.ToString().Trim(),
                        Mensaje = worksheet.Cells[row, 2].Value.ToString().Trim(),
                        Motivo = worksheet.Cells[row, 3].Value.ToString().Trim(),
                        CreatedAt = DateTime.Now,
                        Estado=false,
                        TipoBase= tipoBase
                    });
                }
            } // the using 
            if (list.Count()>0)
            {
                db.DT_Mensajes_Telefonica_DigitalBlue_Bk.AddRange(list);
                db.SaveChanges();
                ViewData["MensajeCarga"] = "Carga Éxitosa";
            }
            return View("Index",list);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
